using System.Net;
using System.Text;
using Remitter.Client;

namespace Remitter.Client.Tests;

public sealed class RemitterApiClientTests
{
    [Fact]
    public async Task GetPaymentsAsync_DeserializesCurrentQueueContract()
    {
        using var http = new HttpClient(new StubHandler("""{"items":[{"id":"p1","invoice_no":513696,"amount":"78.35","pay_type":7,"comment":"","version":3,"check_state":"checked","remit_eligible":true}]}"""))
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var client = new RemitterApiClient(http);
        var items = await client.GetPaymentsAsync();

        var item = Assert.Single(items);
        Assert.Equal(513696, item.InvoiceNo);
        Assert.Equal("78.35", item.Amount);
        Assert.True(item.RemitEligible);
    }

    [Fact]
    public async Task GetOperatorMappingAsync_EncodesWindowsIdentity()
    {
        var handler = new StubHandler("""{"mapping":{"hcn_username":"JGRA","resource_id":467,"display_name":"Jordan Gray","verification":"cached"}}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var client = new RemitterApiClient(http);
        var result = await client.GetOperatorMappingAsync(@"ESC\jordan.grey");

        Assert.NotNull(result.Mapping);
        Assert.Equal("JGRA", result.Mapping!.HcnUsername);
        Assert.Equal(467, result.Mapping.ResourceId);
        Assert.Equal("/api/users/mapping?windows_identity=ESC%5Cjordan.grey", handler.LastRequestUri);
    }

    [Fact]
    public async Task ResolveOperatorAsync_PostsHcnUsername()
    {
        var handler = new StubHandler("""{"user":{"hcn_username":"JGRA","resource_id":467,"display_name":"Jordan Gray","verification":"verified"}}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var client = new RemitterApiClient(http);
        var result = await client.ResolveOperatorAsync("JGRA");

        Assert.Equal("JGRA", result.User.HcnUsername);
        Assert.Equal("POST", handler.LastMethod);
        Assert.Contains("\"hcn_username\":\"JGRA\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task SaveOperatorMappingAsync_PutsWindowsAndHcnUsernames()
    {
        var handler = new StubHandler("""{"user":{"hcn_username":"JGRA","resource_id":467,"display_name":"Jordan Gray","verification":"verified"}}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var client = new RemitterApiClient(http);
        await client.SaveOperatorMappingAsync(@"ESC\jordan.grey", "JGRA");

        Assert.Equal("PUT", handler.LastMethod);
        Assert.Equal("/api/users/mapping", handler.LastRequestUri);
        Assert.Contains("\"windows_identity\":\"ESC\\\\jordan.grey\"", handler.LastRequestBody);
        Assert.Contains("\"hcn_username\":\"JGRA\"", handler.LastRequestBody);
    }


    [Fact]
    public async Task AcquirePaymentLeaseAsync_UsesPaymentLockRoute()
    {
        var handler = new StubHandler("""{"token":"lease-1","version":4,"lease_seconds":30,"payment":{"id":"p1","invoice_no":513696,"amount":"78.35","pay_type":7,"comment":"","version":4}}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var client = new RemitterApiClient(http);
        var lease = await client.AcquirePaymentLeaseAsync("p1");

        Assert.Equal("lease-1", lease.Token);
        Assert.Equal(4, lease.Version);
        Assert.Equal("/api/payments/p1/lock", handler.LastRequestUri);
        Assert.Equal("POST", handler.LastMethod);
    }

    [Fact]
    public async Task SearchInvoicesAsync_PostsBoundedSearchCriteria()
    {
        var handler = new StubHandler("""{"items":[],"next_after":123}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var client = new RemitterApiClient(http);
        var result = await client.SearchInvoicesAsync(new()
        {
            Surname = "Gr",
            IncludeCancelled = true,
            Limit = 20
        });

        Assert.Equal(123, result.NextAfter);
        Assert.Equal("/api/invoices/search", handler.LastRequestUri);
        Assert.Contains("\"surname\":\"Gr\"", handler.LastRequestBody);
        Assert.Contains("\"include_cancelled\":true", handler.LastRequestBody);
        Assert.Contains("\"limit\":20", handler.LastRequestBody);
    }

    [Fact]
    public async Task UpdateAllocationsAsync_PreservesAllExplicitServiceValues()
    {
        var handler = new StubHandler("""{"outcome":"updated","payment":{"id":"p1","amount":"50.00","pay_type":7,"comment":"","version":6}}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var client = new RemitterApiClient(http);
        await client.UpdateAllocationsAsync("p1", new()
        {
            Version = 5,
            LeaseToken = "lease-1",
            AllocationMode = "manual",
            Allocations = new Dictionary<string, string>
            {
                ["10"] = "25.00",
                ["11"] = "25.00",
                ["orphan"] = "0.00"
            }
        });

        Assert.Equal("PATCH", handler.LastMethod);
        Assert.Equal("/api/payments/p1/allocations", handler.LastRequestUri);
        Assert.Contains("\"allocation_mode\":\"manual\"", handler.LastRequestBody);
        Assert.Contains("\"orphan\":\"0.00\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task ReleasePaymentLeaseAsync_AcceptsNoContent()
    {
        var handler = new StubHandler("", HttpStatusCode.NoContent);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var client = new RemitterApiClient(http);
        await client.ReleasePaymentLeaseAsync("p1", "lease-1");

        Assert.Equal("DELETE", handler.LastMethod);
        Assert.Equal("/api/payments/p1/lock", handler.LastRequestUri);
    }

    private sealed class StubHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? LastMethod { get; private set; }
        public string? LastRequestUri { get; private set; }
        public string LastRequestBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastMethod = request.Method.Method;
            LastRequestUri = request.RequestUri?.PathAndQuery;
            if (request.Content is not null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
