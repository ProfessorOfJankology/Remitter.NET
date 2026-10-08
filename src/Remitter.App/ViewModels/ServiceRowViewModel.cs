using Remitter.Domain.Models;
using Remitter.Domain.Presentation;

namespace Remitter.App.ViewModels;

public sealed class ServiceRowViewModel
{
    public string ServiceId { get; }
    public string ItemNumber { get; }
    public string Description { get; }
    public string Fee { get; }
    public string Tax { get; }
    public string Paid { get; }
    public string TaxPaid { get; }
    public string Outstanding { get; }
    public string Allocation { get; }
    public string TaxAllocation { get; }

    public ServiceRowViewModel(ServiceLine service, AllocationPreview? allocation)
    {
        ServiceId = service.ServiceId;
        ItemNumber = service.ItemNumber ?? "";
        Description = service.Description ?? "";
        Fee = PaymentPresentation.Currency(service.Fee);
        Tax = PaymentPresentation.Currency(service.Tax);
        Paid = PaymentPresentation.Currency(service.BasePaid);
        TaxPaid = PaymentPresentation.Currency(service.TaxPaid);
        Outstanding = PaymentPresentation.Currency(service.Outstanding);
        allocation?.Gross?.TryGetValue(service.ServiceId, out var gross);
        allocation?.Tax?.TryGetValue(service.ServiceId, out var tax);
        Allocation = PaymentPresentation.Currency(gross);
        TaxAllocation = PaymentPresentation.Currency(tax);
    }
}
