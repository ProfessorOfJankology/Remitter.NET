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

public sealed class OperatorUser
{
    [JsonPropertyName("hcn_username")] public string HcnUsername { get; set; } = "";
    [JsonPropertyName("resource_id")] public int ResourceId { get; set; }
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("verification")] public string Verification { get; set; } = "";
    [JsonPropertyName("windows_identity")] public string? WindowsIdentity { get; set; }
    [JsonPropertyName("updated_at")] public string? UpdatedAt { get; set; }
}

public sealed class OperatorMappingResponse
{
    [JsonPropertyName("mapping")] public OperatorUser? Mapping { get; set; }
}

public sealed class OperatorUserResponse
{
    [JsonPropertyName("user")] public OperatorUser User { get; set; } = new();
}

public sealed class ResolveOperatorRequest
{
    [JsonPropertyName("hcn_username")] public string HcnUsername { get; set; } = "";
}

public sealed class SaveOperatorMappingRequest
{
    [JsonPropertyName("windows_identity")] public string WindowsIdentity { get; set; } = "";
    [JsonPropertyName("hcn_username")] public string HcnUsername { get; set; } = "";
}


public sealed class PaymentLeaseResponse
{
    [JsonPropertyName("token")] public string Token { get; set; } = "";
    [JsonPropertyName("payment")] public Payment Payment { get; set; } = new();
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("lease_seconds")] public int LeaseSeconds { get; set; } = 30;
}

public sealed class PaymentLeaseRequest
{
    [JsonPropertyName("lease_token")] public string? LeaseToken { get; set; }
}

public sealed class UpdatePaymentRequest
{
    [JsonPropertyName("invoice_no")] public int InvoiceNo { get; set; }
    [JsonPropertyName("amount")] public string Amount { get; set; } = "";
    [JsonPropertyName("pay_type")] public int PayType { get; set; } = 7;
    [JsonPropertyName("comment")] public string Comment { get; set; } = "";
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("lease_token")] public string LeaseToken { get; set; } = "";
}

public sealed class InvoiceSearchRequest
{
    [JsonPropertyName("invoice_no")] public string? InvoiceNo { get; set; }
    [JsonPropertyName("surname")] public string? Surname { get; set; }
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("dob")] public string? Dob { get; set; }
    [JsonPropertyName("reference")] public string? Reference { get; set; }
    [JsonPropertyName("payer")] public string? Payer { get; set; }
    [JsonPropertyName("date")] public string? Date { get; set; }
    [JsonPropertyName("fee")] public string? Fee { get; set; }
    [JsonPropertyName("outstanding")] public string? Outstanding { get; set; }
    [JsonPropertyName("include_cancelled")] public bool IncludeCancelled { get; set; }
    [JsonPropertyName("include_paid")] public bool IncludePaid { get; set; }
    [JsonPropertyName("limit")] public int Limit { get; set; } = 20;
    [JsonPropertyName("after")] public int? After { get; set; }
}

public sealed class InvoiceSearchResponse
{
    [JsonPropertyName("items")] public List<InvoiceSearchItem> Items { get; set; } = [];
    [JsonPropertyName("next_after")] public int? NextAfter { get; set; }
}

public sealed class InvoiceSearchItem
{
    [JsonPropertyName("invoice_no")] public int InvoiceNo { get; set; }
    [JsonPropertyName("surname")] public string? Surname { get; set; }
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("dob")] public string? Dob { get; set; }
    [JsonPropertyName("visit_date")] public string? VisitDate { get; set; }
    [JsonPropertyName("reference")] public string? Reference { get; set; }
    [JsonPropertyName("inv_to_code")] public string? InvToCode { get; set; }
    [JsonPropertyName("item_numbers")] public string? ItemNumbers { get; set; }
    [JsonPropertyName("fee")] public string? Fee { get; set; }
    [JsonPropertyName("amount_paid")] public string? AmountPaid { get; set; }
    [JsonPropertyName("outstanding")] public string? Outstanding { get; set; }
    [JsonPropertyName("search_status")] public string? SearchStatus { get; set; }
}


public sealed class UpdateAllocationsRequest
{
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("lease_token")] public string LeaseToken { get; set; } = "";
    [JsonPropertyName("allocation_mode")] public string AllocationMode { get; set; } = "manual";
    [JsonPropertyName("allocations")] public Dictionary<string, string> Allocations { get; set; } = [];
}
