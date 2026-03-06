using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClinicManager.Database;
using ClinicManager.Helpers;
using ClinicManager.Licensing;
using ClinicManager.Localization;
using ClinicManager.Models;
using ClinicManager.Services;

namespace ClinicManager.ViewModels;

public class SettingsViewModel : ViewModelBase, ILoadable
{
    private readonly SettingsService _settingsService;
    private readonly LicenseManager _licenseManager;
    private readonly DatabaseBackupService _backupService;

    private AppSettings _settings = new();
    private string _machineId = string.Empty;
    private string _licensedTo = string.Empty;
    private string _activationDate = string.Empty;
    private bool _isLicensed;
    private string _selectedLanguage = "en";
    private string _selectedTheme = "Light";
    private bool _isLoading;

    public AppSettings Settings { get => _settings; set => SetProperty(ref _settings, value); }
    public string MachineId { get => _machineId; set => SetProperty(ref _machineId, value); }
    public string LicensedTo { get => _licensedTo; set => SetProperty(ref _licensedTo, value); }
    public string ActivationDate { get => _activationDate; set => SetProperty(ref _activationDate, value); }
    public bool IsLicensed { get => _isLicensed; set => SetProperty(ref _isLicensed, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value))
            {
                Settings.Language = value;
                TranslationSource.Instance.SetLanguage(value);
            }
        }
    }

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                Settings.Theme = value;
                ApplyTheme(value);
            }
        }
    }

    public string ClinicName
    {
        get => _settings.ClinicName;
        set { _settings.ClinicName = value; OnPropertyChanged(); }
    }

    public string ClinicAddress
    {
        get => _settings.ClinicAddress;
        set { _settings.ClinicAddress = value; OnPropertyChanged(); }
    }

    public string ClinicPhone
    {
        get => _settings.ClinicPhone;
        set { _settings.ClinicPhone = value; OnPropertyChanged(); }
    }

    public string DefaultDoctor
    {
        get => _settings.DefaultDoctor;
        set { _settings.DefaultDoctor = value; OnPropertyChanged(); }
    }

    public bool AutoBackup
    {
        get => _settings.AutoBackup;
        set { _settings.AutoBackup = value; OnPropertyChanged(); }
    }

    public bool MinimizeToTray
    {
        get => _settings.MinimizeToTray;
        set { _settings.MinimizeToTray = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }
    public ICommand BackupNowCommand { get; }
    public ICommand RestoreBackupCommand { get; }
    public ICommand BrowseBackupPathCommand { get; }

    public SettingsViewModel(SettingsService settingsService, LicenseManager licenseManager, DatabaseBackupService backupService)
    {
        _settingsService = settingsService;
        _licenseManager = licenseManager;
        _backupService = backupService;

        SaveCommand = new AsyncRelayCommand(SaveSettingsAsync);
        BackupNowCommand = new AsyncRelayCommand(BackupNowAsync);
        RestoreBackupCommand = new AsyncRelayCommand(RestoreBackupAsync);
        BrowseBackupPathCommand = new RelayCommand(BrowseBackupPath);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Settings = await _settingsService.LoadAsync();
            _selectedLanguage = Settings.Language;
            _selectedTheme = Settings.Theme;
            OnPropertyChanged(nameof(SelectedLanguage));
            OnPropertyChanged(nameof(SelectedTheme));
            OnPropertyChanged(nameof(ClinicName));
            OnPropertyChanged(nameof(ClinicAddress));
            OnPropertyChanged(nameof(ClinicPhone));
            OnPropertyChanged(nameof(DefaultDoctor));
            OnPropertyChanged(nameof(AutoBackup));
            OnPropertyChanged(nameof(MinimizeToTray));

            MachineId = _licenseManager.GetMachineId();
            IsLicensed = _licenseManager.IsLicensed();

            var licInfo = _licenseManager.GetLicenseInfo();
            if (licInfo != null)
            {
                LicensedTo = licInfo.LicensedTo;
                ActivationDate = licInfo.ActivationDate.ToString("yyyy-MM-dd");
            }
        }
        finally { IsLoading = false; }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SaveAsync(Settings);
            MessageBox.Show(Localization.Strings.SavedSuccessfully, Localization.Strings.Success, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task BackupNowAsync()
    {
        try
        {
            var path = await _backupService.CreateBackupAsync(
                string.IsNullOrEmpty(Settings.BackupPath) ? null : Settings.BackupPath);
            MessageBox.Show($"{Localization.Strings.BackupCreated}\n{path}", Localization.Strings.Success, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Backup error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RestoreBackupAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Database Files|*.db",
            Title = "Select Backup File"
        };

        if (dialog.ShowDialog() == true)
        {
            var result = MessageBox.Show(
                "Restoring a backup will replace all current data. Continue?",
                Localization.Strings.Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _backupService.RestoreBackupAsync(dialog.FileName);
                MessageBox.Show(Localization.Strings.BackupRestored, Localization.Strings.Success, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void BrowseBackupPath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select backup folder"
        };

        if (dialog.ShowDialog() == true)
        {
            Settings.BackupPath = dialog.FolderName;
            OnPropertyChanged(nameof(Settings));
        }
    }

    private void ApplyTheme(string theme)
    {
        var app = Application.Current;
        if (app == null) return;

        var themeUri = theme == "Dark"
            ? new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative)
            : new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative);

        app.Resources.MergedDictionaries.Clear();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }
}
