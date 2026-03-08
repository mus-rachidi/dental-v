using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class SessionService
{
    private readonly DispatcherTimer _inactivityTimer;
    private readonly TimeSpan _inactivityTimeout = TimeSpan.FromMinutes(15);
    private DateTime _lastActivity = DateTime.UtcNow;
    private Action? _onLogoutRequested;

    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public SessionService()
    {
        _inactivityTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1),
            IsEnabled = false
        };
        _inactivityTimer.Tick += OnInactivityTick;
    }

    public void SetCurrentUser(User? user)
    {
        CurrentUser = user;
        if (user != null)
        {
            _lastActivity = DateTime.UtcNow;
            _inactivityTimer.Start();
        }
        else
        {
            _inactivityTimer.Stop();
        }
    }

    public void RegisterActivity()
    {
        _lastActivity = DateTime.UtcNow;
    }

    public void SetLogoutCallback(Action callback)
    {
        _onLogoutRequested = callback;
    }

    public void Logout()
    {
        _onLogoutRequested?.Invoke();
    }

    public bool HasPermission(Permission permission)
    {
        if (CurrentUser == null) return false;
        var perms = RolePermissions.GetPermissions(CurrentUser.Role);
        return (perms & permission) == permission;
    }

    private void OnInactivityTick(object? sender, EventArgs e)
    {
        if ((DateTime.UtcNow - _lastActivity) >= _inactivityTimeout)
        {
            _inactivityTimer.Stop();
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "You have been logged out due to inactivity.",
                    "Session Expired",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                _onLogoutRequested?.Invoke();
            });
        }
    }

    public static void TrackActivity(UIElement element)
    {
        void Track(object? s, EventArgs _) =>
            App.SessionService?.RegisterActivity();

        element.PreviewMouseMove += Track;
        element.PreviewKeyDown += Track;
    }
}
