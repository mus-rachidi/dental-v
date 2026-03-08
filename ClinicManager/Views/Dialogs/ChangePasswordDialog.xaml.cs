using System.Windows;
using System.Windows.Controls;
using ClinicManager.Models;
using ClinicManager.Services;

namespace ClinicManager.Views.Dialogs;

public partial class ChangePasswordDialog : Window
{
    private readonly AuthService _authService;
    private readonly User _user;

    public bool Success { get; private set; }

    public ChangePasswordDialog(AuthService authService, User user)
    {
        InitializeComponent();
        _authService = authService;
        _user = user;
    }

    private async void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var newPassword = NewPasswordBox.Password;
        var confirm = ConfirmPasswordBox.Password;

        if (string.IsNullOrEmpty(newPassword))
        {
            ShowError("Please enter a new password.");
            return;
        }

        if (newPassword != confirm)
        {
            ShowError("Passwords do not match.");
            return;
        }

        var (success, message) = AuthService.ValidatePassword(newPassword);
        if (!success)
        {
            ShowError(message);
            return;
        }

        OkButton.IsEnabled = false;
        var (resetSuccess, resetMessage) = await _authService.ResetPasswordAsync(_user.Id, newPassword);
        OkButton.IsEnabled = true;

        if (resetSuccess)
        {
            Success = true;
            DialogResult = true;
            Close();
        }
        else
        {
            ShowError(resetMessage);
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
