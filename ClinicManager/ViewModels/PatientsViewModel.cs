using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClinicManager.Helpers;
using ClinicManager.Models;
using ClinicManager.Services;

namespace ClinicManager.ViewModels;

public class PatientsViewModel : ViewModelBase, ILoadable
{
    private readonly PatientService _patientService;
    private readonly ExportService _exportService;
    private readonly SettingsService _settingsService;

    private string _searchQuery = string.Empty;
    private Patient? _selectedPatient;
    private bool _isEditing;
    private bool _isLoading;
    private Patient _editingPatient = new();

    public string SearchQuery
    {
        get => _searchQuery;
        set { SetProperty(ref _searchQuery, value); _ = SearchAsync(); }
    }

    public Patient? SelectedPatient
    {
        get => _selectedPatient;
        set { SetProperty(ref _selectedPatient, value); OnPropertyChanged(nameof(HasSelection)); }
    }

    public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool HasSelection => SelectedPatient != null;

    public Patient EditingPatient
    {
        get => _editingPatient;
        set => SetProperty(ref _editingPatient, value);
    }

    public ObservableCollection<Patient> Patients { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand RefreshCommand { get; }

    public PatientsViewModel(PatientService patientService, ExportService exportService, SettingsService settingsService)
    {
        _patientService = patientService;
        _exportService = exportService;
        _settingsService = settingsService;

        AddCommand = new RelayCommand(StartAdd);
        EditCommand = new RelayCommand(StartEdit, () => HasSelection);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => HasSelection);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        ExportExcelCommand = new AsyncRelayCommand(ExportToExcelAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExportToPdfAsync);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
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

    private void StartAdd()
    {
        EditingPatient = new Patient();
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
            Notes = SelectedPatient.Notes
        };
        IsEditing = true;
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EditingPatient.FullName))
            {
                MessageBox.Show("Patient name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingPatient.Id == 0)
                await _patientService.CreateAsync(EditingPatient);
            else
                await _patientService.UpdateAsync(EditingPatient);

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
    }

    private async Task DeleteAsync()
    {
        if (SelectedPatient == null) return;

        var result = MessageBox.Show(
            Localization.Strings.ConfirmDeletePatient,
            Localization.Strings.Confirm,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            await _patientService.DeleteAsync(SelectedPatient.Id);
            SelectedPatient = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting patient: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
