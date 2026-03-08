using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClinicManager.Helpers;
using ClinicManager.Models;
using ClinicManager.Services;

namespace ClinicManager.ViewModels;

public class ToothViewModel : ViewModelBase
{
    private ToothCondition _condition;
    private string _notes = string.Empty;
    private bool _isSelected;

    public int ToothNumber { get; set; }
    public ToothType Type { get; set; }
    public string Name => ToothService.GetToothName(ToothNumber);

    public ToothCondition Condition
    {
        get => _condition;
        set { SetProperty(ref _condition, value); OnPropertyChanged(nameof(Color)); }
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Color => Condition switch
    {
        ToothCondition.Healthy => "#10B981",
        ToothCondition.Cavity => "#EF4444",
        ToothCondition.Filled => "#3B82F6",
        ToothCondition.Crown => "#F59E0B",
        ToothCondition.RootCanal => "#8B5CF6",
        ToothCondition.Missing => "#6B7280",
        ToothCondition.Implant => "#06B6D4",
        ToothCondition.Bridge => "#EC4899",
        ToothCondition.Extraction => "#DC2626",
        ToothCondition.Fractured => "#F97316",
        _ => "#10B981"
    };
}

public class PatientsViewModel : ViewModelBase, ILoadable
{
    private readonly PatientService _patientService;
    private readonly ExportService _exportService;
    private readonly SettingsService _settingsService;
    private readonly ToothService _toothService;
    private readonly XRayService _xRayService = new();

    private string _searchQuery = string.Empty;
    private Patient? _selectedPatient;
    private bool _isEditing;
    private bool _isLoading;
    private Patient _editingPatient = new();
    private ImageSource? _patientPhoto;
    private ToothViewModel? _selectedTooth;
    private int _healthyCount;
    private int _issueCount;
    private string _selectedPatientTab = "Info";

    public string SearchQuery
    {
        get => _searchQuery;
        set { SetProperty(ref _searchQuery, value); _ = SearchAsync(); }
    }

    public Patient? SelectedPatient
    {
        get => _selectedPatient;
        set
        {
            if (SetProperty(ref _selectedPatient, value))
            {
                OnPropertyChanged(nameof(HasSelection));
                _ = LoadPatientDentalChartAsync();
            }
        }
    }

    public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool HasSelection => SelectedPatient != null;

    public string SelectedPatientTab
    {
        get => _selectedPatientTab;
        set => SetProperty(ref _selectedPatientTab, value);
    }

    public Patient EditingPatient
    {
        get => _editingPatient;
        set => SetProperty(ref _editingPatient, value);
    }

    public ImageSource? PatientPhoto
    {
        get => _patientPhoto;
        set => SetProperty(ref _patientPhoto, value);
    }

    public ToothViewModel? SelectedTooth
    {
        get => _selectedTooth;
        set => SetProperty(ref _selectedTooth, value);
    }

    public int HealthyCount { get => _healthyCount; set => SetProperty(ref _healthyCount, value); }
    public int IssueCount { get => _issueCount; set => SetProperty(ref _issueCount, value); }
    public bool HasXRays => XRays.Count > 0;

    public ObservableCollection<Patient> Patients { get; } = new();
    public ObservableCollection<ToothViewModel> UpperTeeth { get; } = new();
    public ObservableCollection<ToothViewModel> LowerTeeth { get; } = new();
    public ObservableCollection<XRayRecord> XRays { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ChoosePhotoCommand { get; }
    public ICommand SaveToothCommand { get; }
    public ICommand ShowInfoTabCommand { get; }
    public ICommand ShowChartTabCommand { get; }
    public ICommand AddXRayCommand { get; }
    public ICommand DeleteXRayCommand { get; }

    public PatientsViewModel(PatientService patientService, ExportService exportService, SettingsService settingsService)
    {
        _patientService = patientService;
        _exportService = exportService;
        _settingsService = settingsService;
        _toothService = new ToothService();

        AddCommand = new RelayCommand(StartAdd);
        EditCommand = new RelayCommand(StartEdit, () => HasSelection);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => HasSelection);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        ExportExcelCommand = new AsyncRelayCommand(ExportToExcelAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExportToPdfAsync);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        ChoosePhotoCommand = new RelayCommand(ChoosePhoto);
        SaveToothCommand = new AsyncRelayCommand(SaveToothAsync);
        ShowInfoTabCommand = new RelayCommand(() => SelectedPatientTab = "Info");
        ShowChartTabCommand = new RelayCommand(() => SelectedPatientTab = "Chart");
        AddXRayCommand = new RelayCommand(AddXRay, () => HasSelection);
        DeleteXRayCommand = new AsyncRelayCommand(DeleteXRayAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _patientService.GetAllAsync();
            Patients.Clear();
            foreach (var p in list) Patients.Add(p);
        }
        finally { IsLoading = false; }
    }

    private async Task SearchAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _patientService.SearchAsync(SearchQuery);
            Patients.Clear();
            foreach (var p in list) Patients.Add(p);
        }
        finally { IsLoading = false; }
    }

    private async Task LoadPatientDentalChartAsync()
    {
        UpperTeeth.Clear();
        LowerTeeth.Clear();
        XRays.Clear();
        SelectedTooth = null;
        HealthyCount = 0;
        IssueCount = 0;

        if (SelectedPatient == null) return;

        try
        {
            await _toothService.InitializePatientTeethAsync(SelectedPatient.Id);
            var teeth = await _toothService.GetByPatientAsync(SelectedPatient.Id);
            var dict = teeth.ToDictionary(t => t.ToothNumber);

            // Dental chart order (patient's view): Upper 8,7,6,5,4,3,2,1 | 9,10,11,12,13,14,15,16
            var upperOrder = new[] { 8, 7, 6, 5, 4, 3, 2, 1, 9, 10, 11, 12, 13, 14, 15, 16 };
            foreach (var num in upperOrder)
                if (dict.TryGetValue(num, out var t))
                    UpperTeeth.Add(ToVm(t));

            // Lower: 24,23,22,21,20,19,18,17 | 32,31,30,29,28,27,26,25
            var lowerOrder = new[] { 24, 23, 22, 21, 20, 19, 18, 17, 32, 31, 30, 29, 28, 27, 26, 25 };
            foreach (var num in lowerOrder)
                if (dict.TryGetValue(num, out var t))
                    LowerTeeth.Add(ToVm(t));

            HealthyCount = teeth.Count(t => t.Condition == ToothCondition.Healthy);
            IssueCount = teeth.Count(t => t.Condition != ToothCondition.Healthy);

            var xrays = await _xRayService.GetByPatientAsync(SelectedPatient.Id);
            foreach (var x in xrays) XRays.Add(x);
        }
        catch { }
    }

    private static ToothViewModel ToVm(ToothRecord t) => new()
    {
        ToothNumber = t.ToothNumber,
        Type = t.Type,
        Condition = t.Condition,
        Notes = t.Notes
    };

    private async void AddXRay()
    {
        if (SelectedPatient == null) return;
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "Select X-Ray Image"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var path = XRayService.SaveXRayImage(SelectedPatient.Id, dialog.FileName);
            var record = new XRayRecord
            {
                PatientId = SelectedPatient.Id,
                ImagePath = path,
                Date = DateTime.Now
            };
            await _xRayService.SaveAsync(record);
            XRays.Insert(0, record);
            OnPropertyChanged(nameof(HasXRays));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding X-ray: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteXRayAsync(object? parameter)
    {
        if (parameter is not XRayRecord x) return;
        try
        {
            await _xRayService.DeleteAsync(x.Id);
            XRays.Remove(x);
            OnPropertyChanged(nameof(HasXRays));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting X-ray: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void SelectTooth(ToothViewModel tooth)
    {
        if (SelectedTooth != null)
            SelectedTooth.IsSelected = false;

        SelectedTooth = tooth;
        tooth.IsSelected = true;
    }

    private void StartAdd()
    {
        EditingPatient = new Patient();
        PatientPhoto = null;
        IsEditing = true;
    }

    private void StartEdit()
    {
        if (SelectedPatient == null) return;
        EditingPatient = new Patient
        {
            Id = SelectedPatient.Id,
            FullName = SelectedPatient.FullName,
            Phone = SelectedPatient.Phone,
            DateOfBirth = SelectedPatient.DateOfBirth,
            Gender = SelectedPatient.Gender,
            Email = SelectedPatient.Email,
            Address = SelectedPatient.Address,
            Notes = SelectedPatient.Notes,
            PhotoPath = SelectedPatient.PhotoPath,
            CIN = SelectedPatient.CIN,
            EmergencyContact = SelectedPatient.EmergencyContact,
            RegistrationDate = SelectedPatient.RegistrationDate,
            Allergies = SelectedPatient.Allergies,
            Medications = SelectedPatient.Medications,
            ChronicDiseases = SelectedPatient.ChronicDiseases,
            PregnancyStatus = SelectedPatient.PregnancyStatus,
            CNSSNumber = SelectedPatient.CNSSNumber,
            CNSSCoverageType = SelectedPatient.CNSSCoverageType,
            CNSSRegistrationDate = SelectedPatient.CNSSRegistrationDate,
            CNSSValidityDate = SelectedPatient.CNSSValidityDate
        };
        LoadPhoto(EditingPatient.PhotoPath);
        IsEditing = true;
    }

    private void LoadPhoto(string? path)
    {
        PatientPhoto = null;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.DecodePixelWidth = 200;
                bitmap.EndInit();
                bitmap.Freeze();
                PatientPhoto = bitmap;
            }
            catch { }
        }
    }

    private void ChoosePhoto()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "Select Patient Photo"
        };

        if (dialog.ShowDialog() == true)
        {
            EditingPatient.PhotoPath = dialog.FileName;
            LoadPhoto(dialog.FileName);
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EditingPatient.FullName))
            {
                Helpers.NotificationHelper.PatientNameRequired();
                return;
            }

            if (EditingPatient.Id == 0)
            {
                var created = await _patientService.CreateAsync(EditingPatient);
                var uid = App.SessionService?.CurrentUser?.Id;
                if (uid.HasValue) AuditService.Log(uid.Value, AuditService.Actions.CreatePatient, created.FullName);
                if (!string.IsNullOrEmpty(EditingPatient.PhotoPath) && File.Exists(EditingPatient.PhotoPath))
                {
                    var saved = ToothService.SavePatientPhoto(created.Id, EditingPatient.PhotoPath);
                    created.PhotoPath = saved;
                    await _patientService.UpdateAsync(created);
                }
                await _toothService.InitializePatientTeethAsync(created.Id);
            }
            else
            {
                if (!string.IsNullOrEmpty(EditingPatient.PhotoPath) && File.Exists(EditingPatient.PhotoPath)
                    && !EditingPatient.PhotoPath.Contains("ClinicManager"))
                {
                    var saved = ToothService.SavePatientPhoto(EditingPatient.Id, EditingPatient.PhotoPath);
                    EditingPatient.PhotoPath = saved;
                }
                await _patientService.UpdateAsync(EditingPatient);
                var uid = App.SessionService?.CurrentUser?.Id;
                if (uid.HasValue) AuditService.Log(uid.Value, AuditService.Actions.EditPatient, EditingPatient.FullName);
            }

            IsEditing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving patient: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelEdit()
    {
        IsEditing = false;
        PatientPhoto = null;
    }

    private async Task DeleteAsync()
    {
        if (SelectedPatient == null) return;

        var result = MessageBox.Show(
            Localization.Strings.ConfirmDeletePatient,
            Localization.Strings.Confirm,
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            var name = SelectedPatient.FullName;
            await _patientService.DeleteAsync(SelectedPatient.Id);
            var uid = App.SessionService?.CurrentUser?.Id;
            if (uid.HasValue) AuditService.Log(uid.Value, AuditService.Actions.DeletePatient, name);
            SelectedPatient = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting patient: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SaveToothAsync()
    {
        if (SelectedPatient == null || SelectedTooth == null) return;

        try
        {
            var record = new ToothRecord
            {
                PatientId = SelectedPatient.Id,
                ToothNumber = SelectedTooth.ToothNumber,
                Type = SelectedTooth.Type,
                Condition = SelectedTooth.Condition,
                Notes = SelectedTooth.Notes
            };

            await _toothService.SaveAsync(record);

            var allTeeth = UpperTeeth.Concat(LowerTeeth);
            HealthyCount = allTeeth.Count(t => t.Condition == ToothCondition.Healthy);
            IssueCount = allTeeth.Count(t => t.Condition != ToothCondition.Healthy);

            MessageBox.Show("Tooth record saved.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving tooth: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportToExcelAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"Patients_{DateTime.Now:yyyyMMdd}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var all = await _patientService.GetAllAsync();
                await _exportService.ExportPatientsToExcelAsync(all, dialog.FileName);
                MessageBox.Show(Localization.Strings.ExportComplete, Localization.Strings.Success, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task ExportToPdfAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDF Files|*.pdf",
            FileName = $"Patients_{DateTime.Now:yyyyMMdd}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var settings = await _settingsService.LoadAsync();
                var all = await _patientService.GetAllAsync();
                await _exportService.ExportPatientsToPdfAsync(all, dialog.FileName, settings.ClinicName);
                MessageBox.Show(Localization.Strings.ExportComplete, Localization.Strings.Success, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
