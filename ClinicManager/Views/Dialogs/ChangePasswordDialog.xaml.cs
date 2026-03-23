using System;
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
        if (OkButton?.IsEnabled == false) return;
        e.Handled = true;
        ErrorText.Visibility = Visibility.Collapsed;
        var newPassword = NewPasswordBox?.Password ?? string.Empty;
        var confirm = ConfirmPasswordBox?.Password ?? string.Empty;

        if (string.IsNullOrEmpty(newPassword))
        {
            ShowError("Please enter a new password.");
            return;
        }

        if (string.IsNullOrEmpty(confirm))
        {
            ShowError("Please confirm your password.");
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
        OkButton.Content = "Saving...";
        try
        {
            var (resetSuccess, resetMessage) = await _authService.ResetPasswordAsync(_user.Id, newPassword);
            Dispatcher.Invoke(() =>
            {
                if (resetSuccess)
                {
                    Success = true;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    ShowError(resetMessage);
                    OkButton.IsEnabled = true;
                    OkButton.Content = "Accept / Change Password";
                }
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                ShowError(string.IsNullOrEmpty(ex.Message) ? "An error occurred. Please try again." : ex.Message);
                OkButton.IsEnabled = true;
                OkButton.Content = "Accept / Change Password";
            });
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
