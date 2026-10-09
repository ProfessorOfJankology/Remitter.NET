using System.Windows;
using System.Windows.Input;
using Remitter.Client;
using Remitter.Domain.Models;

namespace Remitter.App;

public partial class OperatorDialog : Window
{
    private readonly RemitterApiClient _api;
    private readonly string _windowsIdentity;
    private bool _busy;

    public OperatorUser? SelectedUser { get; private set; }

    public OperatorDialog(
        Window owner,
        RemitterApiClient api,
        string windowsIdentity,
        OperatorUser? currentUser = null)
    {
        InitializeComponent();
        Owner = owner;
        _api = api;
        _windowsIdentity = windowsIdentity;

        WindowsIdentityText.Text = windowsIdentity;
        HcnUsernameBox.Text = currentUser?.HcnUsername ?? "";
        Loaded += (_, _) =>
        {
            HcnUsernameBox.Focus();
            HcnUsernameBox.SelectAll();
        };
    }

    private async void Verify_Click(object sender, RoutedEventArgs e)
        => await VerifyAsync();

    private async Task VerifyAsync()
    {
        if (_busy)
            return;

        var username = HcnUsernameBox.Text.Trim();
        if (username.Length == 0)
        {
            MessageText.Text = "Enter an HCN username.";
            HcnUsernameBox.Focus();
            return;
        }

        try
        {
            SetBusy(true);
            MessageText.Text = "Verifying…";

            var response = RememberCheckBox.IsChecked == true
                ? await _api.SaveOperatorMappingAsync(_windowsIdentity, username)
                : await _api.ResolveOperatorAsync(username);

            SelectedUser = response.User;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageText.Text = SafeMessage(ex);
            HcnUsernameBox.Focus();
            HcnUsernameBox.SelectAll();
        }
        finally
        {
            if (IsVisible)
                SetBusy(false);
        }
    }

    private void HcnUsernameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;
        _ = VerifyAsync();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_busy)
        {
            MessageText.Text = "Wait for the current request to finish before closing.";
            return;
        }

        DialogResult = false;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        HcnUsernameBox.IsEnabled = !busy;
        RememberCheckBox.IsEnabled = !busy;
        VerifyButton.IsEnabled = !busy;
        CancelButton.IsEnabled = !busy;
        Cursor = busy ? Cursors.Wait : null;
    }

    private static string SafeMessage(Exception ex)
        => ex is RemitterApiException api ? api.Message :
           ex is TaskCanceledException ? "No confirmed server response." :
           ex.Message;
}
