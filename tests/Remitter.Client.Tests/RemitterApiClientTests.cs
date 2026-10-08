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

    private sealed class StubHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
    }
}
