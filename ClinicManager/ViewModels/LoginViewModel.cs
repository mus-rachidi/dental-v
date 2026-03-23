using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
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
        set
        {
            if (SetProperty(ref _username, value) && !string.IsNullOrEmpty(_errorMessage))
                ErrorMessage = string.Empty;
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value) && !string.IsNullOrEmpty(_errorMessage))
                ErrorMessage = string.Empty;
        }
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

    public bool IsPasswordVisible
    {
        get => _isPasswordVisible;
        set => SetProperty(ref _isPasswordVisible, value);
    }
    private bool _isPasswordVisible;

    /// <summary>True only on first login - show "Use demo credentials". Hidden after user changes password.</summary>
    public bool ShowDemoCredentials
    {
        get => _showDemoCredentials;
        set => SetProperty(ref _showDemoCredentials, value);
    }
    private bool _showDemoCredentials = true;

    public ICommand LoginCommand { get; }

    public Action<User, bool>? OnLoginSuccess { get; set; }

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
        LoginCommand = new AsyncRelayCommand(LoginAsync, () => !IsLoading);
    }

    /// <summary>Call when login window is loaded - loads whether to show demo credentials.</summary>
    public async Task LoadShowDemoCredentialsAsync()
    {
        try
        {
            var show = await _authService.ShouldShowDemoCredentialsAsync();
            RunOnUiThread(() => ShowDemoCredentials = show);
        }
        catch { RunOnUiThread(() => ShowDemoCredentials = true); } // Show on error so first-time users can login
    }

    /// <summary>Call from view when Sign In is clicked - always runs login (does not depend on CanExecute).</summary>
    public void StartLogin()
    {
        if (_isLoading) return;
        _ = LoginAsync();
    }

    private async Task LoginAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter username and password.";
            return;
        }

        // Set loading on UI thread so button/fields update immediately
        RunOnUiThread(() => { IsLoading = true; });

        try
        {
            LogLoginAttempt(Username, Password?.Length ?? 0);
            var result = await _authService.LoginAsync(Username, Password);
            LogLoginResult(result.Success, result.Message);
            RunOnUiThread(() =>
            {
                if (result.Success && result.User != null)
                {
                    if (OnLoginSuccess != null)
                        OnLoginSuccess.Invoke(result.User, result.MustChangePassword);
                    else
                        ErrorMessage = "Application configuration error. Please restart.";
                }
                else
                {
                    ErrorMessage = result.Message;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
            var displayMsg = GetFullExceptionDisplay(ex);
            RunOnUiThread(() => { ErrorMessage = displayMsg; });
        }
        finally
        {
            RunOnUiThread(() => { IsLoading = false; });
        }
    }

    private static void LogLoginAttempt(string username, int passwordLen)
    {
        var dir = Path.Combine(DataPathHelper.GetClinicManagerDirectory(), "Logs");
        try
        {
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "login_debug.txt");
            var line = $"[{DateTime.Now:HH:mm:ss}] Login attempt: username='{username}', passwordLen={passwordLen}";
            File.AppendAllText(file, line + Environment.NewLine);
            System.Diagnostics.Debug.WriteLine(line);
        }
        catch { }
    }

    private static void LogLoginResult(bool success, string message)
    {
        var dir = Path.Combine(DataPathHelper.GetClinicManagerDirectory(), "Logs");
        try
        {
            var file = Path.Combine(dir, "login_debug.txt");
            var line = $"[{DateTime.Now:HH:mm:ss}] Result: Success={success}, Message={message}";
            File.AppendAllText(file, line + Environment.NewLine);
            System.Diagnostics.Debug.WriteLine(line);
        }
        catch { }
    }

    private static string GetFullExceptionDisplay(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(ex.GetType().Name).Append(": ").Append(ex.Message);
        var inner = ex.InnerException;
        while (inner != null)
        {
            sb.Append("\n\nInner: ").Append(inner.GetType().Name).Append(": ").Append(inner.Message);
            inner = inner.InnerException;
        }
        if (ex.StackTrace != null)
            sb.Append("\n\nStack: ").Append(ex.StackTrace.Replace("\r\n", "\n").Split('\n')[0]);
        return sb.Length > 0 ? sb.ToString() : "An error occurred. Please try again.";
    }

    private static void RunOnUiThread(Action action)
    {
        if (action == null) return;
        var app = Application.Current;
        if (app?.Dispatcher == null) return;
        try
        {
            if (app.Dispatcher.CheckAccess())
                action();
            else
                app.Dispatcher.Invoke(action);
        }
        catch (ObjectDisposedException)
        {
            // Dispatcher or app disposed
        }
        catch (InvalidOperationException)
        {
            // App or dispatcher shutting down
        }
    }
}
