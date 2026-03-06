using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClinicManager.Helpers;
using ClinicManager.Models;
using ClinicManager.Services;

namespace ClinicManager.ViewModels;

public class MedicalRecordsViewModel : ViewModelBase, ILoadable
{
    private readonly MedicalRecordService _recordService;
    private readonly PatientService _patientService;

    private MedicalRecord? _selectedRecord;
    private bool _isEditing;
    private bool _isLoading;
    private string _searchQuery = string.Empty;

    private int _editId;
    private int _editPatientId;
    private DateTime _editDate = DateTime.Today;
    private string _editDoctor = string.Empty;
    private string _editDiagnosis = string.Empty;
    private string _editPrescription = string.Empty;
    private string _editNotes = string.Empty;
    private string _editVitals = string.Empty;

    public MedicalRecord? SelectedRecord
    {
        get => _selectedRecord;
        set { SetProperty(ref _selectedRecord, value); OnPropertyChanged(nameof(HasSelection)); }
    }

    public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool HasSelection => SelectedRecord != null;
    public string SearchQuery { get => _searchQuery; set { SetProperty(ref _searchQuery, value); _ = SearchAsync(); } }

    public int EditPatientId { get => _editPatientId; set => SetProperty(ref _editPatientId, value); }
    public DateTime EditDate { get => _editDate; set => SetProperty(ref _editDate, value); }
    public string EditDoctor { get => _editDoctor; set => SetProperty(ref _editDoctor, value); }
    public string EditDiagnosis { get => _editDiagnosis; set => SetProperty(ref _editDiagnosis, value); }
    public string EditPrescription { get => _editPrescription; set => SetProperty(ref _editPrescription, value); }
    public string EditNotes { get => _editNotes; set => SetProperty(ref _editNotes, value); }
    public string EditVitals { get => _editVitals; set => SetProperty(ref _editVitals, value); }

    public ObservableCollection<MedicalRecord> Records { get; } = new();
    public ObservableCollection<Patient> AvailablePatients { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand RefreshCommand { get; }

    public MedicalRecordsViewModel(MedicalRecordService recordService, PatientService patientService)
    {
        _recordService = recordService;
        _patientService = patientService;

        AddCommand = new AsyncRelayCommand(StartAddAsync);
        EditCommand = new AsyncRelayCommand(StartEditAsync, () => HasSelection);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => HasSelection);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelEditCommand = new RelayCommand(() => IsEditing = false);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _recordService.GetAllAsync();
            Records.Clear();
            foreach (var r in list) Records.Add(r);
        }
        finally { IsLoading = false; }
    }

    private async Task SearchAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _recordService.SearchAsync(SearchQuery);
            Records.Clear();
            foreach (var r in list) Records.Add(r);
        }
        finally { IsLoading = false; }
    }

    private async Task LoadPatientsAsync()
    {
        var patients = await _patientService.GetAllAsync();
        AvailablePatients.Clear();
        foreach (var p in patients) AvailablePatients.Add(p);
    }

    private async Task StartAddAsync()
    {
        await LoadPatientsAsync();
        _editId = 0;
        EditPatientId = AvailablePatients.FirstOrDefault()?.Id ?? 0;
        EditDate = DateTime.Today;
        EditDoctor = string.Empty;
        EditDiagnosis = string.Empty;
        EditPrescription = string.Empty;
        EditNotes = string.Empty;
        EditVitals = string.Empty;
        IsEditing = true;
    }

    private async Task StartEditAsync()
    {
        if (SelectedRecord == null) return;
        await LoadPatientsAsync();

        _editId = SelectedRecord.Id;
        EditPatientId = SelectedRecord.PatientId;
        EditDate = SelectedRecord.Date;
        EditDoctor = SelectedRecord.DoctorName;
        EditDiagnosis = SelectedRecord.Diagnosis;
        EditPrescription = SelectedRecord.Prescription;
        EditNotes = SelectedRecord.Notes;
        EditVitals = SelectedRecord.Vitals;
        IsEditing = true;
    }

    private async Task SaveAsync()
    {
        try
        {
            if (EditPatientId == 0)
            {
                MessageBox.Show("Please select a patient.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var record = new MedicalRecord
            {
                Id = _editId,
                PatientId = EditPatientId,
                Date = EditDate,
                DoctorName = EditDoctor,
                Diagnosis = EditDiagnosis,
                Prescription = EditPrescription,
                Notes = EditNotes,
                Vitals = EditVitals
            };

            if (_editId == 0)
                await _recordService.CreateAsync(record);
            else
                await _recordService.UpdateAsync(record);

            IsEditing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving record: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedRecord == null) return;
        var result = MessageBox.Show(Localization.Strings.ConfirmDelete, Localization.Strings.Confirm, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        await _recordService.DeleteAsync(SelectedRecord.Id);
        SelectedRecord = null;
        await LoadAsync();
    }
}
