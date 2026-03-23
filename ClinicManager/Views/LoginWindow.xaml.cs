using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClinicManager.Services;
using ClinicManager.ViewModels;

namespace ClinicManager.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        TrySetIcon();
        UsernameBox.Focus();
        Loaded += LoginWindow_Loaded;
        Closed += (_, _) => _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= LoginWindow_Loaded;
        await _viewModel.LoadShowDemoCredentialsAsync();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.ErrorMessage) && !string.IsNullOrEmpty(_viewModel.ErrorMessage))
            Dispatcher.BeginInvoke(() => ErrorBorder.BringIntoView());
        if (e.PropertyName == nameof(LoginViewModel.IsLoading) && _viewModel.IsLoading)
            Dispatcher.BeginInvoke(() => ButtonRowBorder.BringIntoView());
    }

    private void TrySetIcon()
    {
        try
        {
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/ClinicManager;component/Resources/Icons/app.ico", UriKind.Absolute));
        }
        catch { /* ignore icon load failure */ }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
            _viewModel.Password = pb.Password;
    }

    private void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        try
        {
            _viewModel.Username = (UsernameBox?.Text ?? string.Empty).Trim();
            _viewModel.Password = ShowPasswordCheck.IsChecked == true
                ? (PasswordTextBox.Text ?? string.Empty)
                : (PasswordBox.Password ?? string.Empty);
            _viewModel.StartLogin();
        }
        catch (Exception ex)
        {
            _viewModel.ErrorMessage = ex.Message ?? "An error occurred. Please try again.";
        }
    }

    private void UsernameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PasswordBox.Focus();
            e.Handled = true;
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _viewModel.Password = PasswordBox.Password ?? string.Empty;
        _viewModel.StartLogin();
        e.Handled = true;
    }

    private void PasswordTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        _viewModel.StartLogin();
        e.Handled = true;
    }

    private void UseDefault_Click(object sender, RoutedEventArgs e)
    {
        var user = AuthService.DemoUsername;
        var pass = AuthService.DemoPassword;
        _viewModel.Username = user;
        _viewModel.Password = pass;
        UsernameBox.Text = user;
        PasswordBox.Password = pass;
        PasswordTextBox.Text = pass;
        _viewModel.StartLogin();
    }

    private void ShowPasswordCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (ShowPasswordCheck.IsChecked == true)
            _viewModel.Password = PasswordBox.Password ?? string.Empty;
        else
            PasswordBox.Password = _viewModel.Password ?? string.Empty;
    }
}
