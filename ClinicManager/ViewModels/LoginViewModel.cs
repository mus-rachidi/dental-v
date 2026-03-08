using System;
using System.Windows.Input;
using ClinicManager.Helpers;
using ClinicManager.Models;
using ClinicManager.Services;

namespace ClinicManager.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public ICommand LoginCommand { get; }

    public Action<User>? OnLoginSuccess { get; set; }

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        LoginCommand = new AsyncRelayCommand(LoginAsync, () => !IsLoading);
    }

    private async System.Threading.Tasks.Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter username and password.";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _authService.LoginAsync(Username, Password);
            if (result.Success && result.User != null)
            {
                OnLoginSuccess?.Invoke(result.User);
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred. Please try again.";
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
