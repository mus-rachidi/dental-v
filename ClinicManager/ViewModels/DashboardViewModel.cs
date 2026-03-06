using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ClinicManager.Helpers;
using ClinicManager.Models;
using ClinicManager.Services;

namespace ClinicManager.ViewModels;

public class DashboardViewModel : ViewModelBase, ILoadable
{
    private readonly PatientService _patientService;
    private readonly AppointmentService _appointmentService;
    private readonly PaymentService _paymentService;

    private int _totalPatients;
    private int _todayAppointments;
    private int _upcomingAppointments;
    private decimal _todayRevenue;
    private decimal _monthRevenue;
    private string _searchQuery = string.Empty;
    private bool _isLoading;

    public int TotalPatients { get => _totalPatients; set => SetProperty(ref _totalPatients, value); }
    public int TodayAppointments { get => _todayAppointments; set => SetProperty(ref _todayAppointments, value); }
    public int UpcomingAppointments { get => _upcomingAppointments; set => SetProperty(ref _upcomingAppointments, value); }
    public decimal TodayRevenue { get => _todayRevenue; set => SetProperty(ref _todayRevenue, value); }
    public decimal MonthRevenue { get => _monthRevenue; set => SetProperty(ref _monthRevenue, value); }
    public string SearchQuery { get => _searchQuery; set { SetProperty(ref _searchQuery, value); _ = SearchPatientsAsync(); } }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    public ObservableCollection<Appointment> TodayAppointmentsList { get; } = new();
    public ObservableCollection<Patient> RecentPatients { get; } = new();
    public ObservableCollection<Patient> SearchResults { get; } = new();

    public Action? NavigateToPatients { get; set; }
    public Action? NavigateToAppointments { get; set; }

    public ICommand RefreshCommand { get; }
    public ICommand ViewAllPatientsCommand { get; }
    public ICommand ViewAllAppointmentsCommand { get; }

    public DashboardViewModel(
        PatientService patientService,
        AppointmentService appointmentService,
        PaymentService paymentService)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
        _paymentService = paymentService;

        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        ViewAllPatientsCommand = new RelayCommand(() => NavigateToPatients?.Invoke());
        ViewAllAppointmentsCommand = new RelayCommand(() => NavigateToAppointments?.Invoke());
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            TotalPatients = await _patientService.GetTotalCountAsync();
            TodayAppointments = await _appointmentService.GetTodayCountAsync();
            UpcomingAppointments = await _appointmentService.GetUpcomingCountAsync();
            TodayRevenue = await _paymentService.GetTodayRevenueAsync();
            MonthRevenue = await _paymentService.GetMonthRevenueAsync();

            var todayAppts = await _appointmentService.GetTodayAsync();
            TodayAppointmentsList.Clear();
            foreach (var a in todayAppts)
                TodayAppointmentsList.Add(a);

            var recent = await _patientService.GetRecentAsync(5);
            RecentPatients.Clear();
            foreach (var p in recent)
                RecentPatients.Add(p);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchPatientsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            return;
        }

        var results = await _patientService.SearchAsync(SearchQuery);
        SearchResults.Clear();
        foreach (var p in results.GetRange(0, Math.Min(results.Count, 10)))
            SearchResults.Add(p);
    }
}
