using Remitter.Domain.Models;
using Remitter.Domain.Presentation;

namespace Remitter.Domain.Tests;

public sealed class PaymentPresentationTests
{
    [Fact]
    public void Currency_FormatsLikeDesktop()
        => Assert.Equal("$1,234.50", PaymentPresentation.Currency("1234.5"));

    [Fact]
    public void Status_ExactPayment_UsesOutstanding()
    {
        var row = new Payment
        {
            InvoiceNo = 513696,
            Amount = "78.35",
            CheckState = "checked",
            Check = new InvoiceCheck { Found = true, Outstanding = "78.35", InvoiceTo = 4 }
        };

        Assert.Equal("Exact payment", PaymentPresentation.Status(row).Status);
    }

    [Fact]
    public void Status_PartialPayment_IsNotError()
    {
        var row = new Payment
        {
            InvoiceNo = 513696,
            Amount = "50.00",
            CheckState = "checked",
            Check = new InvoiceCheck { Found = true, Outstanding = "78.35", InvoiceTo = 4 }
        };

        Assert.Equal("Partial payment", PaymentPresentation.Status(row).Status);
    }

    [Fact]
    public void Status_Overpayment_IsError()
    {
        var row = new Payment
        {
            InvoiceNo = 513696,
            Amount = "80.00",
            CheckState = "checked",
            Check = new InvoiceCheck { Found = true, Outstanding = "78.35", InvoiceTo = 4 }
        };

        Assert.Equal("Overpayment", PaymentPresentation.Status(row).Status);
    }
}
