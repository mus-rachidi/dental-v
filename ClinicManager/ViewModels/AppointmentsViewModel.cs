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

public class AppointmentsViewModel : ViewModelBase, ILoadable
{
    private readonly AppointmentService _appointmentService;
    private readonly PatientService _patientService;

    private Appointment? _selectedAppointment;
    private bool _isEditing;
    private bool _isLoading;
    private DateTime _filterFrom = DateTime.Today;
    private DateTime _filterTo = DateTime.Today.AddDays(7);

    private int _editPatientId;
    private string _editDoctor = string.Empty;
    private DateTime _editDate = DateTime.Today;
    private TimeSpan _editTime = new(9, 0, 0);
    private int _editDuration = 30;
    private AppointmentStatus _editStatus = AppointmentStatus.Scheduled;
    private string _editNotes = string.Empty;
    private int _editId;

    public Appointment? SelectedAppointment
    {
        get => _selectedAppointment;
        set { SetProperty(ref _selectedAppointment, value); OnPropertyChanged(nameof(HasSelection)); }
    }

    public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool HasSelection => SelectedAppointment != null;

    public DateTime FilterFrom { get => _filterFrom; set => SetProperty(ref _filterFrom, value); }
    public DateTime FilterTo { get => _filterTo; set => SetProperty(ref _filterTo, value); }

    public int EditPatientId { get => _editPatientId; set => SetProperty(ref _editPatientId, value); }
    public string EditDoctor { get => _editDoctor; set => SetProperty(ref _editDoctor, value); }
    public DateTime EditDate { get => _editDate; set => SetProperty(ref _editDate, value); }
    public TimeSpan EditTime { get => _editTime; set => SetProperty(ref _editTime, value); }
    public int EditDuration { get => _editDuration; set => SetProperty(ref _editDuration, value); }
    public AppointmentStatus EditStatus { get => _editStatus; set => SetProperty(ref _editStatus, value); }
    public string EditNotes { get => _editNotes; set => SetProperty(ref _editNotes, value); }

    public ObservableCollection<Appointment> Appointments { get; } = new();
    public ObservableCollection<Patient> AvailablePatients { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand ShowTodayCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand MarkCompletedCommand { get; }
    public ICommand MarkCancelledCommand { get; }

    public AppointmentsViewModel(AppointmentService appointmentService, PatientService patientService)
    {
        _appointmentService = appointmentService;
        _patientService = patientService;

        AddCommand = new AsyncRelayCommand(StartAddAsync);
        EditCommand = new AsyncRelayCommand(StartEditAsync, () => HasSelection);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => HasSelection);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelEditCommand = new RelayCommand(CancelEdit);
        FilterCommand = new AsyncRelayCommand(FilterAsync);
        ShowTodayCommand = new AsyncRelayCommand(ShowTodayAsync);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        MarkCompletedCommand = new AsyncRelayCommand(p => UpdateStatusAsync(AppointmentStatus.Completed));
        MarkCancelledCommand = new AsyncRelayCommand(p => UpdateStatusAsync(AppointmentStatus.Cancelled));
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _appointmentService.GetByDateRangeAsync(FilterFrom, FilterTo);
            Appointments.Clear();
            foreach (var a in list) Appointments.Add(a);
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
        EditDoctor = string.Empty;
        EditDate = DateTime.Today;
        EditTime = new TimeSpan(9, 0, 0);
        EditDuration = 30;
        EditStatus = AppointmentStatus.Scheduled;
        EditNotes = string.Empty;
        IsEditing = true;
    }

    private async Task StartEditAsync()
    {
        if (SelectedAppointment == null) return;
        await LoadPatientsAsync();

        _editId = SelectedAppointment.Id;
        EditPatientId = SelectedAppointment.PatientId;
        EditDoctor = SelectedAppointment.DoctorName;
        EditDate = SelectedAppointment.Date;
        EditTime = SelectedAppointment.Time;
        EditDuration = SelectedAppointment.DurationMinutes;
        EditStatus = SelectedAppointment.Status;
        EditNotes = SelectedAppointment.Notes;
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

            var appointment = new Appointment
            {
                Id = _editId,
                PatientId = EditPatientId,
                DoctorName = EditDoctor,
                Date = EditDate,
                Time = EditTime,
                DurationMinutes = EditDuration,
                Status = EditStatus,
                Notes = EditNotes
            };

            if (_editId == 0)
                await _appointmentService.CreateAsync(appointment);
            else
                await _appointmentService.UpdateAsync(appointment);

            IsEditing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving appointment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CancelEdit() => IsEditing = false;

    private async Task DeleteAsync()
    {
        if (SelectedAppointment == null) return;

        var result = MessageBox.Show(
            Localization.Strings.ConfirmDelete,
            Localization.Strings.Confirm,
            MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        await _appointmentService.DeleteAsync(SelectedAppointment.Id);
        SelectedAppointment = null;
        await LoadAsync();
    }

    private async Task FilterAsync() => await LoadAsync();

    private async Task ShowTodayAsync()
    {
        FilterFrom = DateTime.Today;
        FilterTo = DateTime.Today;
        await LoadAsync();
    }

    private async Task UpdateStatusAsync(AppointmentStatus status)
    {
        if (SelectedAppointment == null) return;
        await _appointmentService.UpdateStatusAsync(SelectedAppointment.Id, status);
        await LoadAsync();
    }
}
