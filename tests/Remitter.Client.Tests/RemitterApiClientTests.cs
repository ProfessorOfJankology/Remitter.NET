using System.Net;
using System.Text;
using Remitter.Client;

namespace Remitter.Client.Tests;

public sealed class RemitterApiClientTests
{
    [Fact]
    public async Task GetQueueAsync_DeserializesQueueItems()
    {
        using var http = new HttpClient(new StubHandler("""[{"id":"p1","invoiceNo":513696,"amount":78.35,"status":"Exact"}]"""))
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var client = new RemitterApiClient(http);
        var items = await client.GetQueueAsync();

        var item = Assert.Single(items);
        Assert.Equal(513696, item.InvoiceNo);
        Assert.Equal(78.35m, item.Amount);
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
