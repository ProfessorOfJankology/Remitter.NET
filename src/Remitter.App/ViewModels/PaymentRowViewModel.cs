using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Remitter.Domain.Models;
using Remitter.Domain.Presentation;

namespace Remitter.App.ViewModels;

public sealed class PaymentRowViewModel : INotifyPropertyChanged
{
    private bool _isExpanded;

    public Payment Payment { get; }
    public string Id => Payment.Id;
    public int Version => Payment.Version;
    public string Invoice => Payment.InvoiceNo?.ToString() ?? "";
    public string Amount => PaymentPresentation.Currency(Payment.Amount);
    public string PayType => PaymentPresentation.PayTypeName(Payment.PayType);
    public string Comment => Payment.Comment;
    public string Date => Payment.Check?.VisitDate ?? "";
    public string Name => string.Join(", ", new[] { Payment.Check?.Surname, Payment.Check?.FirstName }.Where(v => !string.IsNullOrWhiteSpace(v)));
    public string Dob => Payment.Check?.Dob ?? "";
    public string Reference => Payment.Check?.Error ?? Payment.Check?.Reference ?? (Payment.Check?.Found == false ? "Invoice not found" : "");
    public string Payer => Payment.Check?.InvToCode ?? "";
    public string Fee => PaymentPresentation.Currency(Payment.Check?.Fee);
    public string Paid => PaymentPresentation.Currency(Payment.Check?.AmountPaid);
    public string Outstanding => PaymentPresentation.Currency(Payment.Check?.Outstanding);
    public string Status { get; }
    public string StatusDetail { get; }
    public bool IsDuplicate => Payment.Duplicate;
    public bool IsCancelled => Payment.Check?.Cancelled == true;
    public bool IsComplete => Payment.ReceiptState == "posted";
    public bool IsError => Status.StartsWith("Error", StringComparison.Ordinal)
        || Status.StartsWith("Overpayment", StringComparison.Ordinal)
        || Status.StartsWith("Allocation error", StringComparison.Ordinal)
        || Status.StartsWith("Allocation review", StringComparison.Ordinal)
        || Status.StartsWith("Unallocated", StringComparison.Ordinal);
    public bool RemitEligible => Payment.RemitEligible;
    public ObservableCollection<ServiceRowViewModel> Services { get; }
    public bool HasServices => Services.Count > 0;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public PaymentRowViewModel(Payment payment)
    {
        Payment = payment;
        (Status, StatusDetail) = PaymentPresentation.Status(payment);
        Services = new((payment.Check?.Services ?? []).Select(s => new ServiceRowViewModel(s, payment.Allocation)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
