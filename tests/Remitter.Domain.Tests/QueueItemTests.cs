using Remitter.Domain.Models;

namespace Remitter.Domain.Tests;

public sealed class QueueItemTests
{
    [Fact]
    public void QueueItem_PreservesCorePaymentFields()
    {
        var item = new QueueItem("payment-1", 513696, 78.35m, "Exact");

        Assert.Equal(513696, item.InvoiceNo);
        Assert.Equal(78.35m, item.Amount);
        Assert.Equal("Exact", item.Status);
    }
}
