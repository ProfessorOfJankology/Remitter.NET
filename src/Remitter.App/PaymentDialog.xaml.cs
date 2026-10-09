using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Remitter.Client;
using Remitter.Domain.Models;
using Remitter.Domain.Presentation;

namespace Remitter.App;

public partial class PaymentDialog : Window
{
    private readonly RemitterApiClient _api;
    private readonly ManualAddRetryStore _manualAdd;
    private PaymentLeaseResponse? _lease;
    private readonly DispatcherTimer _leaseTimer = new();
    private readonly List<int?> _pageAfter = [null];
    private InvoiceSearchRequest? _lastSearch;
    private int _pageIndex;
    private int? _nextAfter;
    private int? _selectedInvoice;
    private bool _busy;
    private bool _leaseActive;

    public ObservableCollection<InvoiceCandidateRow> Candidates { get; } = [];
    public bool Saved { get; private set; }

    public PaymentDialog(
        Window owner,
        RemitterApiClient api,
        ManualAddRetryStore manualAdd,
        PaymentLeaseResponse? lease = null)
    {
        InitializeComponent();
        Owner = owner;
        _api = api;
        _manualAdd = manualAdd;
        _lease = lease;
        _leaseActive = lease is not null;
        CandidatesGrid.ItemsSource = Candidates;

        var payment = lease?.Payment;
        _selectedInvoice = payment?.InvoiceNo;
        InvoiceBox.Text = payment?.InvoiceNo?.ToString() ?? "";
        AmountBox.Text = payment?.Amount ?? "";
        CommentBox.Text = payment?.Comment ?? "";
        SelectPayType(payment?.PayType ?? 7);

        Title = payment is null
            ? "Add Payment"
            : payment.IdentificationState is "unmatched" or "needs_review"
                ? "Resolve Invoice"
                : "Edit Payment";

        UpdateAssociation();
        SourceText.Text = BuildSourceText(payment);

        if (lease is not null)
        {
            var seconds = Math.Max(3, lease.LeaseSeconds / 3);
            _leaseTimer.Interval = TimeSpan.FromSeconds(seconds);
            _leaseTimer.Tick += LeaseTimer_Tick;
            _leaseTimer.Start();
        }

        Closed += PaymentDialog_Closed;
        Loaded += (_, _) =>
        {
            InvoiceBox.Focus();
            InvoiceBox.SelectAll();
        };
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
        => await SearchAsync(newPage: true);

    private async void Search_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        await SearchAsync(newPage: true);
    }

    private async Task SearchAsync(bool newPage)
    {
        if (_busy)
            return;

        InvoiceSearchRequest request;
        if (newPage)
        {
            request = BuildSearchRequest();
            if (!HasSearchCriterion(request))
            {
                NoticeText.Text = "Enter at least one invoice search criterion.";
                return;
            }

            _lastSearch = request;
            _pageAfter.Clear();
            _pageAfter.Add(null);
            _pageIndex = 0;
        }
        else
        {
            if (_lastSearch is null)
                return;
            request = CloneSearch(_lastSearch);
            request.After = _pageAfter[_pageIndex];
        }

        try
        {
            SetBusy(true);
            NoticeText.Text = "Searching HCN invoices…";
            var result = await _api.SearchInvoicesAsync(request);

            Candidates.Clear();
            foreach (var item in result.Items)
                Candidates.Add(new InvoiceCandidateRow(item));

            _nextAfter = result.NextAfter;
            NextButton.IsEnabled = _nextAfter.HasValue;
            PreviousButton.IsEnabled = _pageIndex > 0;
            CandidateNotice.Text = $"{Candidates.Count} candidates. Select the correct invoice explicitly; search does not change the payment.";
            NoticeText.Text = "";
        }
        catch (Exception ex)
        {
            NoticeText.Text = SafeMessage(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private InvoiceSearchRequest BuildSearchRequest()
        => new()
        {
            InvoiceNo = EmptyToNull(InvoiceBox.Text),
            Surname = EmptyToNull(SurnameBox.Text),
            FirstName = EmptyToNull(FirstNameBox.Text),
            Dob = EmptyToNull(DobBox.Text),
            Reference = EmptyToNull(ReferenceBox.Text),
            Payer = EmptyToNull(PayerBox.Text),
            Date = EmptyToNull(VisitDateBox.Text),
            Fee = EmptyToNull(FeeBox.Text),
            Outstanding = EmptyToNull(OutstandingBox.Text),
            IncludeCancelled = IncludeCancelledCheckBox.IsChecked == true,
            IncludePaid = IncludePaidCheckBox.IsChecked == true,
            Limit = 20
        };

    private static InvoiceSearchRequest CloneSearch(InvoiceSearchRequest value)
        => new()
        {
            InvoiceNo = value.InvoiceNo,
            Surname = value.Surname,
            FirstName = value.FirstName,
            Dob = value.Dob,
            Reference = value.Reference,
            Payer = value.Payer,
            Date = value.Date,
            Fee = value.Fee,
            Outstanding = value.Outstanding,
            IncludeCancelled = value.IncludeCancelled,
            IncludePaid = value.IncludePaid,
            Limit = value.Limit
        };

    private static bool HasSearchCriterion(InvoiceSearchRequest value)
        => new[] { value.InvoiceNo, value.Surname, value.FirstName, value.Dob, value.Reference,
                   value.Payer, value.Date, value.Fee, value.Outstanding }
            .Any(v => !string.IsNullOrWhiteSpace(v));

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        foreach (var box in new[] { InvoiceBox, SurnameBox, FirstNameBox, DobBox, ReferenceBox, PayerBox, VisitDateBox, FeeBox, OutstandingBox })
            box.Clear();
        Candidates.Clear();
        CandidateNotice.Text = "";
        NoticeText.Text = "";
        _lastSearch = null;
        _nextAfter = null;
        _pageIndex = 0;
        _pageAfter.Clear();
        _pageAfter.Add(null);
        PreviousButton.IsEnabled = false;
        NextButton.IsEnabled = false;
    }

    private async void Next_Click(object sender, RoutedEventArgs e)
    {
        if (!_nextAfter.HasValue)
            return;
        if (_pageAfter.Count <= _pageIndex + 1)
            _pageAfter.Add(_nextAfter);
        _pageIndex++;
        await SearchAsync(newPage: false);
    }

    private async void Previous_Click(object sender, RoutedEventArgs e)
    {
        if (_pageIndex <= 0)
            return;
        _pageIndex--;
        await SearchAsync(newPage: false);
    }

    private void CandidatesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CandidatesGrid.SelectedItem is not InvoiceCandidateRow row)
            return;
        _selectedInvoice = row.InvoiceNo;
        UpdateAssociation();
    }

    private void CandidatesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CandidatesGrid.SelectedItem is InvoiceCandidateRow)
            UseOutstanding();
    }

    private void UseOutstanding_Click(object sender, RoutedEventArgs e)
        => UseOutstanding();

    private void UseOutstanding()
    {
        if (CandidatesGrid.SelectedItem is not InvoiceCandidateRow row)
        {
            NoticeText.Text = "Select an invoice before using its outstanding amount.";
            return;
        }

        if (string.IsNullOrWhiteSpace(row.Raw.Outstanding))
        {
            NoticeText.Text = "Selected invoice has no outstanding amount.";
            return;
        }

        AmountBox.Text = row.Raw.Outstanding;
        AmountBox.Focus();
        AmountBox.SelectAll();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
        => await SaveAsync();

    private async void Save_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        if (_busy)
            return;
        if (!_selectedInvoice.HasValue || string.IsNullOrWhiteSpace(AmountBox.Text))
        {
            NoticeText.Text = "Select an HCN invoice and enter an amount before saving.";
            return;
        }

        var payType = int.Parse(((ComboBoxItem)PayTypeBox.SelectedItem).Tag!.ToString()!);

        try
        {
            SetBusy(true);
            NoticeText.Text = "Saving payment…";

            if (_lease is null)
            {
                await _manualAdd.AddAsync(
                    _api,
                    _selectedInvoice.Value.ToString(),
                    AmountBox.Text.Trim(),
                    payType,
                    CommentBox.Text);
            }
            else
            {
                await _api.UpdatePaymentAsync(_lease.Payment.Id, new UpdatePaymentRequest
                {
                    InvoiceNo = _selectedInvoice.Value,
                    Amount = AmountBox.Text.Trim(),
                    PayType = payType,
                    Comment = CommentBox.Text,
                    Version = _lease.Version,
                    LeaseToken = _lease.Token
                });
                _leaseActive = false;
            }

            Saved = true;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            NoticeText.Text = SafeMessage(ex);
        }
        finally
        {
            if (IsVisible)
                SetBusy(false);
        }
    }

    private async void LeaseTimer_Tick(object? sender, EventArgs e)
    {
        if (_lease is null || !_leaseActive || _busy)
            return;

        try
        {
            await _api.AcquirePaymentLeaseAsync(_lease.Payment.Id, _lease.Token);
        }
        catch
        {
            NoticeText.Text = "Editing lease could not be renewed. Save will revalidate ownership; copy edits before closing if needed.";
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

    private void PaymentDialog_Closed(object? sender, EventArgs e)
    {
        _leaseTimer.Stop();
        if (_lease is not null && _leaseActive)
            _ = ReleaseLeaseBestEffortAsync(_lease.Payment.Id, _lease.Token);
    }

    private async Task ReleaseLeaseBestEffortAsync(string paymentId, string token)
    {
        try
        {
            await _api.ReleasePaymentLeaseAsync(paymentId, token);
        }
        catch
        {
            // The server lease expires if release cannot be confirmed.
        }
    }

    private void UpdateAssociation()
        => AssociationText.Text = $"Current HCN invoice: {_selectedInvoice?.ToString() ?? "unassigned"}";

    private void SelectPayType(int payType)
    {
        foreach (var item in PayTypeBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag?.ToString() == payType.ToString())
            {
                PayTypeBox.SelectedItem = item;
                return;
            }
        }
        PayTypeBox.SelectedIndex = 0;
    }

    private static string BuildSourceText(Payment? payment)
    {
        if (payment is null || payment.SourceMetadata is null || payment.SourceMetadata.Count == 0)
            return "";

        return $"Source: {payment.Source ?? ""} | Original invoice: {Meta(payment, "original_invoice")} | Reference: {Meta(payment, "reference")}";
    }

    private static string Meta(Payment payment, string key)
    {
        if (payment.SourceMetadata is null || !payment.SourceMetadata.TryGetValue(key, out var value) || value is null)
            return "";
        return value is JsonElement json
            ? json.ValueKind == JsonValueKind.String ? json.GetString() ?? "" : json.ToString()
            : value.ToString() ?? "";
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        SaveButton.IsEnabled = !busy;
        CancelButton.IsEnabled = !busy;
        CandidatesGrid.IsEnabled = !busy;
        Cursor = busy ? Cursors.Wait : null;
    }

    private static string? EmptyToNull(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SafeMessage(Exception ex)
        => ex is RemitterApiException api ? api.Message :
           ex is TaskCanceledException ? "No confirmed server response." :
           ex.Message;

    public sealed class InvoiceCandidateRow
    {
        public InvoiceSearchItem Raw { get; }
        public int InvoiceNo => Raw.InvoiceNo;
        public string Name => string.Join(", ", new[] { Raw.Surname, Raw.FirstName }.Where(v => !string.IsNullOrWhiteSpace(v)));
        public string? Dob => Raw.Dob;
        public string? VisitDate => Raw.VisitDate;
        public string? Reference => Raw.Reference;
        public string? Payer => Raw.InvToCode;
        public string? ItemNumbers => Raw.ItemNumbers;
        public string Fee => PaymentPresentation.Currency(Raw.Fee);
        public string Paid => PaymentPresentation.Currency(Raw.AmountPaid);
        public string Outstanding => PaymentPresentation.Currency(Raw.Outstanding);
        public string Status => Raw.SearchStatus ?? "Review";

        public InvoiceCandidateRow(InvoiceSearchItem raw) => Raw = raw;
    }
}
