using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClinicManager.Services;
using ClinicManager.ViewModels;

namespace ClinicManager.Views;

public partial class MainWindow : Window
{
    private bool _sidebarCollapsed;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += MainWindow_Loaded;
        InputBindings.Add(new KeyBinding(
            new ToggleSidebarCommand(this),
            Key.B, ModifierKeys.Control));
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var user = App.SessionService?.CurrentUser;
        CurrentUserText.Text = user != null ? $"{user.Username} ({user.Role})" : "";
        SessionService.TrackActivity(this);
    }

    private void LogoutBtn_Click(object sender, RoutedEventArgs e)
    {
        App.SessionService?.Logout();
    }

    private async void LangCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || LangCombo.SelectedIndex < 0) return;
        var lang = LangCombo.SelectedIndex == 1 ? "fr" : "en";
        vm.SettingsVM.ApplyLanguage(lang);
        await vm.SettingsVM.SaveSettingsSilentAsync();
    }

    private async void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || ThemeCombo.SelectedIndex < 0) return;
        var theme = ThemeCombo.SelectedIndex == 1 ? "Dark" : "Light";
        vm.SettingsVM.ApplyThemeFromUI(theme);
        await vm.SettingsVM.SaveSettingsSilentAsync();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var settings = ((MainViewModel)DataContext).SettingsVM;
        if (settings.MinimizeToTray)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
        }
        else
        {
            base.OnClosing(e);
        }
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        ToggleSidebar();
    }

    private void ToggleSidebar()
    {
        _sidebarCollapsed = !_sidebarCollapsed;
        var vis = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;

        SidebarColumn.Width = new GridLength(_sidebarCollapsed ? 48 : 220);

        BrandingPanel.Visibility = vis;
        FooterText.Visibility = vis;

        LabelOverview.Visibility = vis;
        LabelManagement.Visibility = vis;
        LabelFinance.Visibility = vis;
        LabelSystem.Visibility = vis;

        NavDashboard.Visibility = vis;
        NavPatients.Visibility = vis;
        NavAppointments.Visibility = vis;
        NavStaff.Visibility = vis;
        NavMedicalRecords.Visibility = vis;
        NavBilling.Visibility = vis;
        NavInventory.Visibility = vis;
        NavReports.Visibility = vis;
        NavSettings.Visibility = vis;
        if (NavUsers != null) NavUsers.Visibility = vis; // Users label (when admin)

        ToggleIcon.Text = _sidebarCollapsed ? "\uE76C" : "\uE700";
    }

    private class ToggleSidebarCommand : ICommand
    {
        private readonly MainWindow _window;
        public ToggleSidebarCommand(MainWindow window) => _window = window;
        public event System.EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _window.ToggleSidebar();
    }
}
