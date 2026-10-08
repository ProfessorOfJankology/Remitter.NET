using System.Text.Json.Serialization;

namespace Remitter.Domain.Models;

public sealed class PaymentsResponse
{
    [JsonPropertyName("items")] public List<Payment> Items { get; set; } = [];
}

public sealed class HealthResponse
{
    [JsonPropertyName("receipting_enabled")] public bool ReceiptingEnabled { get; set; }
}

public sealed class PaymentMutationResponse
{
    [JsonPropertyName("outcome")] public string? Outcome { get; set; }
    [JsonPropertyName("payment")] public Payment Payment { get; set; } = new();
}

public sealed class BatchResultResponse
{
    [JsonPropertyName("items")] public List<BatchResultItem> Items { get; set; } = [];
}

public sealed class BatchResultItem
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("outcome")] public string? Outcome { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
}

public sealed record PaymentVersion(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] int Version);

public sealed class AddPaymentRequest
{
    [JsonPropertyName("invoice_no")] public string InvoiceNo { get; set; } = "";
    [JsonPropertyName("amount")] public string Amount { get; set; } = "";
    [JsonPropertyName("pay_type")] public int PayType { get; set; } = 7;
    [JsonPropertyName("comment")] public string Comment { get; set; } = "";
    [JsonPropertyName("source")] public string Source { get; set; } = "manual";
    [JsonPropertyName("operation_id")] public string OperationId { get; set; } = Guid.NewGuid().ToString();
}

public sealed class CheckPaymentsRequest
{
    [JsonPropertyName("scope")] public string Scope { get; set; } = "all";
    [JsonPropertyName("items")] public List<PaymentVersion>? Items { get; set; }
}

public sealed class RemovePaymentsRequest
{
    [JsonPropertyName("items")] public List<PaymentVersion> Items { get; set; } = [];
}
