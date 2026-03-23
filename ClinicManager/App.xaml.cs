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
            try
            {
                var licenseManager = _serviceProvider.GetRequiredService<LicenseManager>();
                if (!licenseManager.IsLicensed())
                {
                    var dialog = new LicenseDialog(licenseManager);
                    var result = dialog.ShowDialog();

                    if (result != true)
                    {
                        Shutdown();
                        return;
                    }
                    // Proceed if activated or user chose "Continue without license"
                }
            }
            catch (Exception ex)
            {
                try
                {
                    var logDir = Path.Combine(ClinicManager.Helpers.DataPathHelper.GetClinicManagerDirectory(), "Logs");
                    Directory.CreateDirectory(logDir);
                    File.AppendAllText(
                        Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] License startup: {ex}\n\n");
                }
                catch { /* ignore */ }

                MessageBox.Show(
                    $"Could not complete license verification:\n\n{ex.Message}\n\nThe application will continue. You can try activating from Settings if the problem persists.",
                    "License",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            // Ensure default admin exists (admin / Admin@123)
            var authService = _serviceProvider.GetRequiredService<AuthService>();
            try
            {
                await authService.EnsureAdminExistsAsync();
            }
            catch (Exception ex)
            {
                var logDir = Path.Combine(ClinicManager.Helpers.DataPathHelper.GetClinicManagerDirectory(), "Logs");
                try { Directory.CreateDirectory(logDir); } catch { }
                try
                {
                    File.AppendAllText(Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.log"),
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] EnsureAdmin failed: {ex}\n\n");
                }
                catch { }
                // Continue to login screen - user can try admin/Admin@123 and we'll create on-the-fly
            }

            // Show sign-in, then main window on success
            SessionService = _serviceProvider.GetRequiredService<SessionService>();
            var loginVm = _serviceProvider.GetRequiredService<LoginViewModel>();
            var loginWindow = new LoginWindow(loginVm);

            loginVm.OnLoginSuccess = (user, mustChangePassword) =>
            {
                if (mustChangePassword)
                {
                    var auth = _serviceProvider!.GetRequiredService<AuthService>();
                    var changePwdDialog = new ChangePasswordDialog(auth, user);
                    if (changePwdDialog.ShowDialog() != true || !changePwdDialog.Success)
                        return;
                }

                SessionService!.SetCurrentUser(user);
                SessionService.SetLogoutCallback(() =>
                {
                    AuditService.Log(user.Id, AuditService.Actions.Logout);
                    SessionService.SetCurrentUser(null);
                    if (MainWindow is MainWindow mw)
                        mw.IsLoggingOut = true;
                    MainWindow?.Close();
                    var newLogin = _serviceProvider!.GetRequiredService<LoginViewModel>();
                    newLogin.OnLoginSuccess = loginVm.OnLoginSuccess;
                    var newLoginWin = new LoginWindow(newLogin);
                    MainWindow = newLoginWin;
                    newLoginWin.Show();
                    newLoginWin.Activate();
                });

                AuditService.Log(user.Id, AuditService.Actions.Login);

                var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
                var mainWindow = new MainWindow(mainVm);
                var currentWindow = Application.Current.MainWindow;
                mainWindow.Show();
                if (currentWindow != null && currentWindow != mainWindow)
                    currentWindow.Close();
                MainWindow = mainWindow;
                mainVm.Initialize();
                StartScheduledBackup(settings);
            };

            MainWindow = loginWindow;
            loginWindow.Show();
        }
        catch (Exception ex)
        {
            var fullMessage = GetFullExceptionMessage(ex);
            MessageBox.Show($"Failed to start application:\n\n{fullMessage}",
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
        var logDir = GetLogDirectory();
        try
        {
            Directory.CreateDirectory(logDir);
        }
        catch (UnauthorizedAccessException)
        {
            logDir = Path.Combine(Path.GetTempPath(), "ClinicManager", "Logs");
            try { Directory.CreateDirectory(logDir); } catch { /* best effort */ }
        }
        catch (IOException)
        {
            logDir = Path.Combine(Path.GetTempPath(), "ClinicManager", "Logs");
            try { Directory.CreateDirectory(logDir); } catch { /* best effort */ }
        }

        var capturedLogDir = logDir;
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            LogError(capturedLogDir, ex);
            var msg = ex != null ? GetFullExceptionMessage(ex) : "Unknown error";
            MessageBox.Show($"An unexpected error occurred:\n\n{msg}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            LogError(capturedLogDir, args.Exception);
            var msg = GetFullExceptionMessage(args.Exception);
            MessageBox.Show($"An unexpected error occurred:\n\n{msg}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }

    private static string GetLogDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClinicManager", "Logs");
    }

    private static string GetFullExceptionMessage(Exception ex)
    {
        var msg = ex.Message;
        var inner = ex.InnerException;
        while (inner != null)
        {
            msg += $"\n\nInner: {inner.GetType().Name}: {inner.Message}";
            inner = inner.InnerException;
        }
        return msg;
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
