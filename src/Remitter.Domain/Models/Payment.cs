using System.Text.Json.Serialization;

namespace Remitter.Domain.Models;

public sealed class Payment
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("invoice_no")] public int? InvoiceNo { get; set; }
    [JsonPropertyName("amount")] public string Amount { get; set; } = "";
    [JsonPropertyName("pay_type")] public int PayType { get; set; }
    [JsonPropertyName("comment")] public string Comment { get; set; } = "";
    [JsonPropertyName("source")] public string? Source { get; set; }
    [JsonPropertyName("version")] public int Version { get; set; }
    [JsonPropertyName("duplicate")] public bool Duplicate { get; set; }
    [JsonPropertyName("check_state")] public string? CheckState { get; set; }
    [JsonPropertyName("checked_version")] public int? CheckedVersion { get; set; }
    [JsonPropertyName("identification_state")] public string? IdentificationState { get; set; }
    [JsonPropertyName("receipt_state")] public string? ReceiptState { get; set; }
    [JsonPropertyName("receipt_details")] public ReceiptDetails? ReceiptDetails { get; set; }
    [JsonPropertyName("remit_eligible")] public bool RemitEligible { get; set; }
    [JsonPropertyName("remit_exclusion")] public string? RemitExclusion { get; set; }
    [JsonPropertyName("check")] public InvoiceCheck? Check { get; set; }
    [JsonPropertyName("allocation_mode")] public string? AllocationMode { get; set; }
    [JsonPropertyName("allocation_plan")] public AllocationPlan? AllocationPlan { get; set; }
    [JsonPropertyName("allocation")] public AllocationPreview? Allocation { get; set; }
    [JsonPropertyName("source_metadata")] public Dictionary<string, object?>? SourceMetadata { get; set; }
}

public sealed class ReceiptDetails
{
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("receipt_no")] public string? ReceiptNo { get; set; }
}

public sealed class InvoiceCheck
{
    [JsonPropertyName("found")] public bool? Found { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("reference")] public string? Reference { get; set; }
    [JsonPropertyName("visit_date")] public string? VisitDate { get; set; }
    [JsonPropertyName("surname")] public string? Surname { get; set; }
    [JsonPropertyName("first_name")] public string? FirstName { get; set; }
    [JsonPropertyName("dob")] public string? Dob { get; set; }
    [JsonPropertyName("inv_to_code")] public string? InvToCode { get; set; }
    [JsonPropertyName("invoice_to")] public int? InvoiceTo { get; set; }
    [JsonPropertyName("fee")] public string? Fee { get; set; }
    [JsonPropertyName("amount_paid")] public string? AmountPaid { get; set; }
    [JsonPropertyName("outstanding")] public string? Outstanding { get; set; }
    [JsonPropertyName("cancelled")] public bool Cancelled { get; set; }
    [JsonPropertyName("invalid_count")] public int InvalidCount { get; set; }
    [JsonPropertyName("mismatch_count")] public int MismatchCount { get; set; }
    [JsonPropertyName("warnings")] public List<string>? Warnings { get; set; }
    [JsonPropertyName("services")] public List<ServiceLine>? Services { get; set; }
}

public sealed class ServiceLine
{
    [JsonPropertyName("service_id")] public string ServiceId { get; set; } = "";
    [JsonPropertyName("item_number")] public string? ItemNumber { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("fee")] public string? Fee { get; set; }
    [JsonPropertyName("tax")] public string? Tax { get; set; }
    [JsonPropertyName("base_paid")] public string? BasePaid { get; set; }
    [JsonPropertyName("tax_paid")] public string? TaxPaid { get; set; }
    [JsonPropertyName("outstanding")] public string? Outstanding { get; set; }
    [JsonPropertyName("cancelled")] public int Cancelled { get; set; }
}

public sealed class AllocationPreview
{
    [JsonPropertyName("state")] public string? State { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("gross")] public Dictionary<string, string>? Gross { get; set; }
    [JsonPropertyName("tax")] public Dictionary<string, string>? Tax { get; set; }
}


public sealed class AllocationPlan
{
    [JsonPropertyName("invoice_no")] public int? InvoiceNo { get; set; }
    [JsonPropertyName("allocations")] public Dictionary<string, string>? Allocations { get; set; }
}
