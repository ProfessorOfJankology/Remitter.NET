using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Remitter.App.Configuration;
using Remitter.App.ViewModels;
using Remitter.Client;
using Remitter.Domain.Models;
using Remitter.Domain.Presentation;

namespace Remitter.App;

public partial class MainWindow : Window
{
    private RemitterApiClient? _api;
    private ManualAddRetryStore? _manualAdd;
    private RemitterConfig? _config;
    private readonly DispatcherTimer _refreshTimer = new();
    private readonly string _windowsIdentity =
        ((Environment.UserDomainName + "\\" + Environment.UserName).Trim('\\')).ToLowerInvariant();
    private OperatorUser? _currentOperator;
    private bool _busy;

    public ObservableCollection<PaymentRowViewModel> Rows { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _config = RemitterConfig.Load(RemitterConfig.ConfigPath);
            Topmost = _config.AlwaysOnTop;

            var http = new HttpClient
            {
                BaseAddress = new Uri(_config.BaseUrl),
                Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
            };

            _api = new RemitterApiClient(http, _config.Token, _windowsIdentity, "desktop-dotnet:" + Environment.MachineName);
            _manualAdd = new ManualAddRetryStore(_config.BaseUrl);

            _refreshTimer.Interval = TimeSpan.FromSeconds(_config.RefreshSeconds);
            _refreshTimer.Tick += async (_, _) => await RefreshAsync(true);
            _refreshTimer.Start();

            await RefreshAsync(false);
            await LoadOperatorAsync(promptIfMissing: true);
        }
        catch (Exception ex)
        {
            SetStatus("Configuration error: " + ex.Message, Brushes.Firebrick);
        }
    }

    private async Task RefreshAsync(bool quiet)
    {
        if (_api is null || _busy)
            return;

        try
        {
            SetBusy(true, quiet ? null : "Refreshing shared queue...");
            var selected = PaymentsGrid.SelectedItems.OfType<PaymentRowViewModel>().Select(r => r.Id).ToHashSet();
            var expanded = Rows.Where(r => r.IsExpanded).Select(r => r.Id).ToHashSet();

            var healthTask = _api.GetHealthAsync();
            var paymentsTask = _api.GetPaymentsAsync();
            await Task.WhenAll(healthTask, paymentsTask);

            Rows.Clear();
            foreach (var payment in paymentsTask.Result)
            {
                var row = new PaymentRowViewModel(payment) { IsExpanded = expanded.Contains(payment.Id) };
                Rows.Add(row);
            }

            PaymentsGrid.UpdateLayout();
            foreach (var row in Rows.Where(r => selected.Contains(r.Id)))
                PaymentsGrid.SelectedItems.Add(row);

            var notice = BuildQueueNotice(Rows);
            notice += healthTask.Result.ReceiptingEnabled
                ? " Receipting enabled."
                : " Receipting disabled.";
            SetStatus(notice, NoticeBrush(Rows));
        }
        catch (Exception ex)
        {
            if (!quiet)
                MessageBox.Show(this, SafeMessage(ex), "Remitter", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Shared queue unavailable; use Refresh to retry. " + SafeMessage(ex), Brushes.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static string BuildQueueNotice(IEnumerable<PaymentRowViewModel> rows)
    {
        var list = rows.ToList();
        var protectedCount = list.Count(r => r.Payment.ReceiptState is "processing" or "uncertain");
        if (protectedCount > 0)
            return protectedCount + " payment(s) processing or requiring receipt reconciliation; edits and resubmission blocked.";

        var rejected = list.Count(r => r.Payment.ReceiptState == "rejected");
        if (rejected > 0)
            return rejected + " receipt attempt(s) rejected. Current eligibility follows the latest check.";

        var failed = list.Count(r => !string.IsNullOrWhiteSpace(r.Payment.Check?.Error));
        if (failed > 0)
            return failed + " invoice check(s) failed. Use Recheck Selected / All to retry.";

        var warnings = list.Count(r => (r.Payment.Check?.Warnings?.Count ?? 0) > 0);
        if (warnings > 0)
            return warnings + " invoice check(s) have warnings; results are provisional.";

        var pending = list.Count(r => r.Payment.CheckState is "pending" or "checking");
        if (pending > 0)
            return "Checking " + pending + " payment(s) in the background.";

        return "Shared queue connected.";
    }

    private static Brush NoticeBrush(IEnumerable<PaymentRowViewModel> rows)
    {
        var list = rows.ToList();
        if (list.Any(r => r.Payment.ReceiptState is "processing" or "uncertain") ||
            list.Any(r => !string.IsNullOrWhiteSpace(r.Payment.Check?.Error)))
            return Brushes.Firebrick;
        if (list.Any(r => r.Payment.ReceiptState == "rejected") ||
            list.Any(r => (r.Payment.Check?.Warnings?.Count ?? 0) > 0))
            return Brushes.DarkOrange;
        return Brushes.ForestGreen;
    }

    private async Task LoadOperatorAsync(bool promptIfMissing)
    {
        if (_api is null)
            return;

        try
        {
            var response = await _api.GetOperatorMappingAsync(_windowsIdentity);
            if (response.Mapping is not null)
            {
                AcceptOperator(response.Mapping);
                return;
            }

            if (promptIfMissing)
                ShowOperatorDialog();
            else
                SetOperatorUnresolved();
        }
        catch (Exception)
        {
            SetOperatorUnresolved();
            SetStatus("User mapping unavailable. Use HCN user to retry; queue remains available.", Brushes.DarkOrange);
        }
    }

    private void AcceptOperator(OperatorUser user)
    {
        _currentOperator = user;
        var name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.HcnUsername : user.DisplayName;
        OperatorButton.Content = $"HCN: {name} ({user.HcnUsername})";
        Title = $"Remitter.NET - {name} ({user.HcnUsername})";
    }

    private void SetOperatorUnresolved()
    {
        _currentOperator = null;
        OperatorButton.Content = "HCN user unresolved";
        Title = "Remitter.NET";
    }

    private void ShowOperatorDialog()
    {
        if (_api is null || _busy)
            return;

        var dialog = new OperatorDialog(this, _api, _windowsIdentity, _currentOperator);
        if (dialog.ShowDialog() == true && dialog.SelectedUser is not null)
        {
            AcceptOperator(dialog.SelectedUser);
            SetStatus($"HCN user set to {dialog.SelectedUser.DisplayName} ({dialog.SelectedUser.HcnUsername}).", Brushes.ForestGreen);
        }
        else if (_currentOperator is null)
        {
            SetOperatorUnresolved();
            SetStatus("HCN user unresolved. Queue remains available.", Brushes.Goldenrod);
        }
    }

    private void ChangeUser_Click(object sender, RoutedEventArgs e)
        => ShowOperatorDialog();

    private async void Refresh_Click(object sender, RoutedEventArgs e)
        => await RefreshAsync(false);

    private async void QuickAdd_Click(object sender, RoutedEventArgs e)
        => await AddQuickEntryAsync();

    private async Task AddQuickEntryAsync()
    {
        if (_api is null || _manualAdd is null || _busy)
            return;

        var invoice = InvoiceEntry.Text.Trim();
        var amount = AmountEntry.Text.Trim();
        if (invoice.Length == 0 || amount.Length == 0)
        {
            MessageBox.Show(this, "Invoice number and amount are required.", "Add payment",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var payType = int.Parse(((ComboBoxItem)PayTypeEntry.SelectedItem).Tag!.ToString()!);
        try
        {
            SetBusy(true, "Adding payment...");
            await _manualAdd.AddAsync(_api, invoice, amount, payType, CommentEntry.Text);
            InvoiceEntry.Clear();
            AmountEntry.Clear();
            CommentEntry.Clear();
            InvoiceEntry.Focus();
            SetStatus("Payment saved. Invoice check queued.", Brushes.ForestGreen);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                SafeMessage(ex) + "\n\nRetry the unchanged payment. Its operation identity has been retained.",
                "Add payment", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Payment add not confirmed.", Brushes.Firebrick);
        }
        finally
        {
            SetBusy(false);
        }

        await RefreshAsync(true);
    }

    private async void QuickEntry_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        e.Handled = true;
        await AddQuickEntryAsync();
    }

    private async void AddToolbar_Click(object sender, RoutedEventArgs e)
    {
        if (_api is null || _manualAdd is null || _busy)
            return;

        var dialog = new PaymentDialog(this, _api, _manualAdd);
        if (dialog.ShowDialog() == true && dialog.Saved)
        {
            SetStatus("Payment saved. Invoice check queued.", Brushes.ForestGreen);
            await RefreshAsync(true);
        }
    }

    private async void Recheck_Click(object sender, RoutedEventArgs e)
    {
        if (_api is null || _busy)
            return;

        var selected = PaymentsGrid.SelectedItems.OfType<PaymentRowViewModel>().ToList();
        var request = selected.Count == 0
            ? new CheckPaymentsRequest { Scope = "all" }
            : new CheckPaymentsRequest
            {
                Scope = "selected",
                Items = selected.Select(r => new PaymentVersion(r.Id, r.Version)).ToList()
            };

        try
        {
            SetBusy(true, selected.Count == 0 ? "Checking whole queue..." : "Checking selected rows...");
            var result = await _api.CheckPaymentsAsync(request);
            var errors = result.Items.Where(i => !string.IsNullOrWhiteSpace(i.Message)).Select(i => i.Message!).ToArray();
            if (errors.Length > 0)
                MessageBox.Show(this, string.Join("\n", errors), "Check results", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetStatus("Checks queued. Results will appear automatically.", Brushes.ForestGreen);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, SafeMessage(ex), "Recheck", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }

        await RefreshAsync(true);
    }

    private async void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        if (_api is null || _busy)
            return;

        var selected = PaymentsGrid.SelectedItems.OfType<PaymentRowViewModel>().ToList();
        if (selected.Count == 0)
            return;

        if (MessageBox.Show(this, "Remove " + selected.Count + " selected payments from the queue?",
                "Remove Selected", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            SetBusy(true, "Removing selected rows...");
            var response = await _api.RemovePaymentsAsync(new RemovePaymentsRequest
            {
                Items = selected.Select(r => new PaymentVersion(r.Id, r.Version)).ToList()
            });
            var errors = response.Items.Where(i => i.Outcome != "removed" && !string.IsNullOrWhiteSpace(i.Message))
                .Select(i => i.Message!).ToArray();
            if (errors.Length > 0)
                MessageBox.Show(this, string.Join("\n", errors), "Rows not removed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, SafeMessage(ex), "Remove Selected", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }

        await RefreshAsync(true);
    }

    private async void CleanPaid_Click(object sender, RoutedEventArgs e)
    {
        if (_api is null || _busy)
            return;

        var paid = Rows.Where(r =>
                r.Payment.CheckState == "checked" &&
                r.Payment.CheckedVersion == r.Payment.Version &&
                PaymentPresentation.Money(r.Payment.Check?.Outstanding) == 0)
            .ToList();

        if (paid.Count == 0)
        {
            SetStatus("No current checked payments with zero outstanding to clean.", Brushes.Goldenrod);
            return;
        }

        try
        {
            SetBusy(true, "Cleaning paid payments...");
            await _api.RemovePaymentsAsync(new RemovePaymentsRequest
            {
                Items = paid.Select(r => new PaymentVersion(r.Id, r.Version)).ToList()
            });
            SetStatus("Cleaned " + paid.Count + " paid payment(s).", Brushes.ForestGreen);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, SafeMessage(ex), "Clean Paid", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
        }

        await RefreshAsync(true);
    }

    private void Status_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not PaymentRowViewModel row)
            return;
        MessageBox.Show(this, row.StatusDetail, "Payment status", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void PaymentsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_api is null || _manualAdd is null || _busy || PaymentsGrid.SelectedItem is not PaymentRowViewModel row)
            return;

        PaymentLeaseResponse lease;
        try
        {
            SetBusy(true, "Acquiring edit lease…");
            lease = await _api.AcquirePaymentLeaseAsync(row.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, SafeMessage(ex), "Cannot edit payment", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("Payment could not be opened for editing.", Brushes.Firebrick);
            return;
        }
        finally
        {
            SetBusy(false);
        }

        var dialog = new PaymentDialog(this, _api, _manualAdd, lease);
        dialog.ShowDialog();
        await RefreshAsync(true);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                ArgumentList = { RemitterConfig.ConfigPath },
                UseShellExecute = true
            });
            SetStatus("Opened config.ini. Connection changes take effect after restart.", Brushes.Goldenrod);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, SafeMessage(ex), "Settings", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SetBusy(bool busy, string? message = null)
    {
        _busy = busy;
        PaymentsGrid.IsEnabled = !busy;
        if (message is not null)
            StatusText.Text = message;
        Cursor = busy ? Cursors.Wait : null;
    }

    private void SetStatus(string message, Brush brush)
    {
        StatusText.Text = message;
        StatusDot.Fill = brush;
    }

    private static string SafeMessage(Exception ex)
        => ex is RemitterApiException api ? api.Message :
           ex is TaskCanceledException ? "No confirmed server response." :
           ex.Message;
}
