using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Remitter.Client;
using Remitter.Domain.Models;
using Remitter.Domain.Presentation;

namespace Remitter.App;

public partial class AllocationDialog : Window
{
    private readonly RemitterApiClient _api;
    private readonly PaymentLeaseResponse _lease;
    private readonly Dictionary<string, string> _allValues;
    private bool _busy;
    private bool _leaseActive = true;

    public ObservableCollection<AllocationRow> Rows { get; } = [];
    public bool Saved { get; private set; }

    public AllocationDialog(Window owner, RemitterApiClient api, PaymentLeaseResponse lease)
    {
        InitializeComponent();
        Owner = owner;
        _api = api;
        _lease = lease;

        var payment = lease.Payment;
        _allValues = new Dictionary<string, string>(
            payment.Allocation?.Gross ?? new Dictionary<string, string>(),
            StringComparer.Ordinal);

        foreach (var service in payment.Check?.Services ?? [])
        {
            if (!_allValues.ContainsKey(service.ServiceId))
                _allValues[service.ServiceId] = "0.00";

            payment.Allocation?.Tax?.TryGetValue(service.ServiceId, out var taxAllocation);
            Rows.Add(new AllocationRow(service, _allValues[service.ServiceId], taxAllocation));
        }

        ServicesGrid.ItemsSource = Rows;
        SummaryText.Text = $"Invoice {payment.InvoiceNo?.ToString() ?? "unassigned"} | Payment {PaymentPresentation.Currency(payment.Amount)} | Mode: {payment.AllocationMode ?? "automatic"}";

        if (payment.AllocationMode == "manual" &&
            payment.AllocationPlan?.InvoiceNo.HasValue == true &&
            payment.AllocationPlan.InvoiceNo != payment.InvoiceNo)
        {
            NoticeText.Text = "Invoice changed. Reset to Automatic before assigning a service plan to another invoice.";
            SaveButton.IsEnabled = false;
        }
        else
        {
            NoticeText.Text = "Edit gross allocations only. Tax is recalculated and validated by the server.";
        }

        Closed += AllocationDialog_Closed;
    }

    private void Fill_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not AllocationRow row)
            return;

        try
        {
            CommitGridEdit();
            var payment = ParseMoney(_lease.Payment.Amount);
            decimal other = 0;
            foreach (var candidate in Rows)
            {
                if (ReferenceEquals(candidate, row))
                    continue;
                other += ParseMoney(candidate.Allocation);
            }

            foreach (var orphan in _allValues.Where(kv => Rows.All(r => r.ServiceId != kv.Key)))
                other += ParseMoney(orphan.Value);

            var available = payment - other;
            if (available < 0)
                throw new InvalidOperationException("Other service allocations exceed the payment. Reduce them first.");
            if (row.Cancelled)
                throw new InvalidOperationException("Cannot allocate to a cancelled or unknown service.");

            row.Allocation = Math.Min(ParseMoney(row.OutstandingRaw), available).ToString("0.00", CultureInfo.InvariantCulture);
            ServicesGrid.Items.Refresh();
            NoticeText.Text = "Filled this service with the available amount.";
        }
        catch (Exception ex)
        {
            NoticeText.Text = ex.Message;
        }
    }

    private void Zero_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not AllocationRow row)
            return;
        row.Allocation = "0.00";
        ServicesGrid.Items.Refresh();
    }

    private void ServicesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.Column.Header?.ToString() != "Gross Allocation" || e.EditingElement is not TextBox box)
            return;

        try
        {
            _ = ParseMoney(box.Text);
            NoticeText.Text = "";
        }
        catch (Exception ex)
        {
            NoticeText.Text = ex.Message;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
        => await SaveAsync("manual");

    private async void Reset_Click(object sender, RoutedEventArgs e)
        => await SaveAsync("automatic");

    private async Task SaveAsync(string mode)
    {
        if (_busy)
            return;

        try
        {
            CommitGridEdit();

            var values = new Dictionary<string, string>(_allValues, StringComparer.Ordinal);
            if (mode == "manual")
            {
                foreach (var row in Rows)
                    values[row.ServiceId] = ParseMoney(row.Allocation).ToString("0.00", CultureInfo.InvariantCulture);
            }
            else
            {
                values.Clear();
            }

            SetBusy(true);
            NoticeText.Text = mode == "automatic" ? "Resetting service allocation…" : "Saving service allocation…";

            await _api.UpdateAllocationsAsync(_lease.Payment.Id, new UpdateAllocationsRequest
            {
                Version = _lease.Version,
                LeaseToken = _lease.Token,
                AllocationMode = mode,
                Allocations = values
            });

            _leaseActive = false;
            Saved = true;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            NoticeText.Text = "Allocation save not confirmed: " + SafeMessage(ex) + " Refresh before retrying.";
        }
        finally
        {
            if (IsVisible)
                SetBusy(false);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            NoticeText.Text = "Wait for the current request to finish before closing.";
            return;
        }
        DialogResult = false;
    }

    private void AllocationDialog_Closed(object? sender, EventArgs e)
    {
        if (_leaseActive)
            _ = ReleaseLeaseBestEffortAsync();
    }

    private async Task ReleaseLeaseBestEffortAsync()
    {
        try
        {
            await _api.ReleasePaymentLeaseAsync(_lease.Payment.Id, _lease.Token);
        }
        catch
        {
            // Server-side expiry is the fallback.
        }
    }

    private void CommitGridEdit()
    {
        ServicesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        ServicesGrid.CommitEdit(DataGridEditingUnit.Row, true);
    }

    private static decimal ParseMoney(string? text)
    {
        if (!decimal.TryParse((text ?? "").Replace("$", "").Replace(",", ""),
                NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ||
            value < 0 || decimal.Round(value, 2) != value)
            throw new InvalidOperationException("Enter a non-negative amount with at most two decimal places.");
        return value;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        ServicesGrid.IsEnabled = !busy;
        SaveButton.IsEnabled = !busy;
        ResetButton.IsEnabled = !busy;
        CancelButton.IsEnabled = !busy;
    }

    private static string SafeMessage(Exception ex)
        => ex is RemitterApiException api ? api.Message :
           ex is TaskCanceledException ? "No confirmed server response." :
           ex.Message;

    public sealed class AllocationRow
    {
        public string ServiceId { get; }
        public string ItemNumber { get; }
        public string Description { get; }
        public string OutstandingRaw { get; }
        public string OutstandingDisplay { get; }
        public string TaxAllocationDisplay { get; }
        public bool Cancelled { get; }
        public string Allocation { get; set; }

        public AllocationRow(ServiceLine service, string allocation, string? taxAllocation)
        {
            ServiceId = service.ServiceId;
            ItemNumber = service.ItemNumber ?? "";
            Description = service.Description ?? "";
            OutstandingRaw = service.Outstanding ?? "0.00";
            OutstandingDisplay = PaymentPresentation.Currency(service.Outstanding);
            TaxAllocationDisplay = PaymentPresentation.Currency(taxAllocation);
            Cancelled = service.Cancelled != 0;
            Allocation = allocation;
        }
    }
}
