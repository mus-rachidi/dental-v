using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ClinicManager.Database;
using ClinicManager.Licensing;
using ClinicManager.Localization;
using ClinicManager.Services;
using ClinicManager.ViewModels;
using ClinicManager.Views;
using ClinicManager.Views.Dialogs;

namespace ClinicManager;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public static SessionService? SessionService { get; private set; }
    private System.Windows.Threading.DispatcherTimer? _backupTimer;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SetupLogging();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        try
        {
            // Initialize database
            using var db = new ClinicDbContext();
            db.EnsureCreated();

            // Load settings and apply language/theme
            var settingsService = _serviceProvider.GetRequiredService<SettingsService>();
            var settings = await settingsService.LoadAsync();
            TranslationSource.Instance.SetLanguage(settings.Language);
            ApplyTheme(settings.Theme);

            // License check
            var licenseManager = _serviceProvider.GetRequiredService<LicenseManager>();
            if (!licenseManager.IsLicensed())
            {
                var dialog = new LicenseDialog(licenseManager);
                var result = dialog.ShowDialog();

                if (result != true || !dialog.IsActivated)
                {
                    Shutdown();
                    return;
                }
            }

            // Ensure default admin exists
            var authService = _serviceProvider.GetRequiredService<AuthService>();
            await authService.EnsureAdminExistsAsync();

            // Authentication - show login first
            SessionService = _serviceProvider.GetRequiredService<SessionService>();
            var loginVm = _serviceProvider.GetRequiredService<LoginViewModel>();
            var loginWindow = new LoginWindow(loginVm);

            loginVm.OnLoginSuccess = (user, mustChangePassword) =>
            {
                if (mustChangePassword)
                {
                    var authService = _serviceProvider!.GetRequiredService<AuthService>();
                    var changePwdDialog = new ChangePasswordDialog(authService, user);
                    if (changePwdDialog.ShowDialog() != true || !changePwdDialog.Success)
                        return;
                }

                SessionService!.SetCurrentUser(user);
                SessionService.SetLogoutCallback(() =>
                {
                    AuditService.Log(user.Id, AuditService.Actions.Logout);
                    SessionService.SetCurrentUser(null);
                    MainWindow?.Close();
                    var newLogin = _serviceProvider!.GetRequiredService<LoginViewModel>();
                    newLogin.OnLoginSuccess = loginVm.OnLoginSuccess;
                    var newLoginWin = new LoginWindow(newLogin);
                    MainWindow = newLoginWin;
                    newLoginWin.Show();
                });

                AuditService.Log(user.Id, AuditService.Actions.Login);

                var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
                var mainWindow = new MainWindow(mainVm);
                MainWindow = mainWindow;
                mainWindow.Show();
                loginWindow.Close();
                mainVm.Initialize();
                StartScheduledBackup(settings);
            };

            MainWindow = loginWindow;
            loginWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start application:\n{ex.Message}",
                "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Services
        services.AddSingleton<AuthService>();
        services.AddSingleton<SessionService>();
        services.AddSingleton<UserService>();
        services.AddSingleton<PatientService>();
        services.AddSingleton<AppointmentService>();
        services.AddSingleton<PaymentService>();
        services.AddSingleton<MedicalRecordService>();
        services.AddSingleton<ToothService>();
        services.AddSingleton<XRayService>();
        services.AddSingleton<StaffService>();
        services.AddSingleton<InventoryService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<DatabaseBackupService>();
        services.AddSingleton<LicenseManager>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<LoginViewModel>();
    }

    private void StartScheduledBackup(Models.AppSettings settings)
    {
        if (!settings.AutoBackup || _serviceProvider == null) return;

        var intervalHours = settings.BackupIntervalHours <= 0 ? 24 : settings.BackupIntervalHours;
        _backupTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromHours(intervalHours)
        };
        _backupTimer.Tick += async (s, _) =>
        {
            try
            {
                var backupService = _serviceProvider.GetRequiredService<DatabaseBackupService>();
                var path = await backupService.CreateBackupAsync(
                    string.IsNullOrEmpty(settings.BackupPath) ? null : settings.BackupPath);
                System.Diagnostics.Debug.WriteLine($"Auto-backup completed: {path}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-backup failed: {ex.Message}");
            }
        };
        _backupTimer.Start();
    }

    private void ApplyTheme(string theme)
    {
        var themeUri = theme == "Dark"
            ? new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative)
            : new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative);

        Resources.MergedDictionaries.Clear();
        Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }

    private void SetupLogging()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClinicManager", "Logs");
        Directory.CreateDirectory(logDir);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            LogError(logDir, ex);
            MessageBox.Show($"An unexpected error occurred:\n{ex?.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            LogError(logDir, args.Exception);
            MessageBox.Show($"An unexpected error occurred:\n{args.Exception.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }

    private static void LogError(string logDir, Exception? ex)
    {
        try
        {
            var logFile = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.log");
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n";
            File.AppendAllText(logFile, entry);
        }
        catch { /* Logging should never crash the app */ }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _backupTimer?.Stop();
        base.OnExit(e);
    }
}
