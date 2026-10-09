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

    private sealed class StubHandler(string responseBody) : HttpMessageHandler
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

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
