using System.Globalization;
using Remitter.Domain.Models;

namespace Remitter.Domain.Presentation;

public static class PaymentPresentation
{
    public static readonly IReadOnlyDictionary<int, string> PayTypeNames =
        new Dictionary<int, string> { [7] = "Direct Deposit", [0] = "Cash", [8] = "EFTPOS Manual" };

    public static decimal? Money(string? value)
        => decimal.TryParse((value ?? "").Replace("$", "").Replace(",", ""),
            NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var amount)
            ? amount : null;

    public static string Currency(string? value)
        => Money(value) is decimal amount ? amount.ToString("$#,##0.00", CultureInfo.InvariantCulture) : "";

    public static string PayTypeName(int value)
        => PayTypeNames.TryGetValue(value, out var name) ? name : value.ToString(CultureInfo.InvariantCulture);

    public static (string Status, string Detail) Status(Payment row)
    {
        var check = row.Check;
        var invoice = row.InvoiceNo?.ToString(CultureInfo.InvariantCulture) ?? "";
        var amount = Money(row.Amount);

        if (row.ReceiptState == "processing")
            return ("Processing…", "Invoice " + invoice + "\nReceipt operation is in progress.");
        if (row.ReceiptState == "uncertain")
            return ("Review", "Invoice " + invoice + "\nReceipt outcome is uncertain and requires reconciliation.");
        if (row.ReceiptState == "posted")
        {
            var suffix = string.IsNullOrWhiteSpace(row.ReceiptDetails?.ReceiptNo) ? "" : " Receipt " + row.ReceiptDetails!.ReceiptNo + ".";
            return ("Complete", "Invoice " + invoice + "\nReceipt posted successfully." + suffix + "\nThis payment is locked against editing and remitting again.");
        }
        if (row.IdentificationState is "unmatched" or "needs_review")
            return (row.IdentificationState == "unmatched" ? "Unmatched" : "Review",
                "Invoice " + invoice + "\nDouble-click to search and assign the correct HCN invoice.");
        if (row.ReceiptState == "rejected" && row.CheckState != "checked")
            return ("Error", "Invoice " + invoice + "\n" + (row.ReceiptDetails?.Message ?? "Receipt rejected.") + "\nA fresh invoice check is required.");
        if (amount is null or <= 0)
            return ("Error", "Invoice " + invoice + "\nPayment must be greater than zero for receipting.");
        if (row.CheckState is "pending" or "checking")
            return ("Checking…", "Invoice " + invoice + "\nInvoice check is pending.");
        if (!string.IsNullOrWhiteSpace(check?.Error))
            return ("Check failed", "Invoice " + invoice + "\n" + check.Error);
        if (check is null)
            return ("Check failed", "Invoice " + invoice + "\nInvoice information is unavailable. Recheck this payment.");
        if (check.Found == false)
            return ("Unmatched", "Invoice " + invoice + "\nInvoice was not found. Double-click to search and assign it.");

        var outstanding = Money(check.Outstanding);
        if (amount is decimal payment && outstanding is decimal balance && payment > balance)
            return ("Overpayment",
                "Invoice " + invoice + "\nProposed payment $" + payment.ToString("0.00") +
                " exceeds invoice outstanding $" + balance.ToString("0.00") + " by $" +
                (payment - balance).ToString("0.00") + ".\nExcess: $" + (payment - balance).ToString("0.00") + ".");

        var allocationStatus = row.Allocation?.State switch
        {
            "review" => "Allocation review",
            "error" => "Allocation error",
            "unallocated" => "Unallocated",
            "unavailable" => "Check failed",
            _ => null
        };
        if (allocationStatus is not null)
            return (allocationStatus, "Invoice " + invoice + "\n" + (row.Allocation?.Message ?? ""));

        var reasons = new List<string>();
        if (check.Cancelled) reasons.Add("Invoice is cancelled.");
        if (check.InvoiceTo is not null && check.InvoiceTo != 4) reasons.Add("Invoice is not eligible for this remittance workflow.");
        if (check.InvalidCount != 0 || check.MismatchCount != 0) reasons.Add("HCN financial records are inconsistent.");
        if (check.Warnings is not null) reasons.AddRange(check.Warnings);

        if (reasons.Count > 0)
        {
            var detail = "Invoice " + invoice + "\n" + string.Join("\n", reasons);
            if (amount is decimal p && outstanding is decimal o)
                detail += "\nProposed payment: $" + p.ToString("0.00") + "\nOutstanding balance: $" + o.ToString("0.00");
            return ("Error", detail);
        }

        if (outstanding is null)
            return ("Check failed", "Invoice " + invoice + "\nOutstanding balance is unavailable. Recheck this payment.");

        var status = amount == outstanding ? "Exact payment" : "Partial payment";
        var detailText = "Invoice " + invoice +
            "\nProposed payment: $" + amount.Value.ToString("0.00") +
            "\nOutstanding balance: $" + outstanding.Value.ToString("0.00") +
            "\nRemaining after payment: $" + (outstanding.Value - amount.Value).ToString("0.00");

        if (row.ReceiptState == "rejected")
        {
            status += " ⚠";
            detailText += "\nPrevious receipt attempt rejected: " + (row.ReceiptDetails?.Message ?? "Receipt rejected.") +
                "\nCurrent eligibility is determined by the latest check.";
        }

        return (status, detailText);
    }
}
