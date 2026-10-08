namespace Remitter.Domain.Models;

public sealed record QueueItem(
    string Id,
    int InvoiceNo,
    decimal Amount,
    string Status,
    string? Comment = null,
    string? PayType = null,
    string? Source = null);
