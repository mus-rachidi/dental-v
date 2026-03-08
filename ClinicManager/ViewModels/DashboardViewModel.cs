using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ClinicManager.Helpers;
using ClinicManager.Models;
using ClinicManager.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ClinicManager.ViewModels;

public class DashboardViewModel : ViewModelBase, ILoadable
{
    private readonly PatientService _patientService;
    private readonly AppointmentService _appointmentService;
    private readonly PaymentService _paymentService;
    private readonly ToothService _toothService;

    private int _totalPatients;
    private int _newPatientsThisMonth;
    private int _todayAppointments;
    private int _upcomingAppointments;
    private int _completedTreatments;
    private decimal _todayRevenue;
    private decimal _monthRevenue;
    private decimal _pendingBills;
    private string _searchQuery = string.Empty;
    private string _alertMessage = string.Empty;
    private bool _hasAlerts;
    private bool _isLoading;
    private DateTime _filterFrom = DateTime.Today.AddMonths(-5);
    private DateTime _filterTo = DateTime.Today;

    public int TotalPatients { get => _totalPatients; set => SetProperty(ref _totalPatients, value); }
    public int NewPatientsThisMonth { get => _newPatientsThisMonth; set => SetProperty(ref _newPatientsThisMonth, value); }
    public int TodayAppointments { get => _todayAppointments; set => SetProperty(ref _todayAppointments, value); }
    public int UpcomingAppointments { get => _upcomingAppointments; set => SetProperty(ref _upcomingAppointments, value); }
    public int CompletedTreatments { get => _completedTreatments; set => SetProperty(ref _completedTreatments, value); }
    public decimal TodayRevenue { get => _todayRevenue; set => SetProperty(ref _todayRevenue, value); }
    public decimal MonthRevenue { get => _monthRevenue; set => SetProperty(ref _monthRevenue, value); }
    public decimal PendingBills { get => _pendingBills; set => SetProperty(ref _pendingBills, value); }
    public string SearchQuery { get => _searchQuery; set { SetProperty(ref _searchQuery, value); _ = SearchPatientsAsync(); } }
    public string AlertMessage { get => _alertMessage; set => SetProperty(ref _alertMessage, value); }
    public bool HasAlerts { get => _hasAlerts; set => SetProperty(ref _hasAlerts, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public DateTime FilterFrom { get => _filterFrom; set => SetProperty(ref _filterFrom, value); }
    public DateTime FilterTo { get => _filterTo; set => SetProperty(ref _filterTo, value); }

    public ObservableCollection<Appointment> TodayAppointmentsList { get; } = new();
    public ObservableCollection<Patient> RecentPatients { get; } = new();
    public ObservableCollection<Patient> SearchResults { get; } = new();

    // Charts
    public ObservableCollection<ISeries> RevenueSeries { get; } = new();
    public ObservableCollection<ISeries> DoctorPieSeries { get; } = new();
    public ObservableCollection<ISeries> TreatmentPieSeries { get; } = new();
    public ObservableCollection<ISeries> DemographicsPieSeries { get; } = new();
    public Axis[] RevenueXAxes { get; } = { new Axis() };

    public Action? NavigateToPatients { get; set; }
    public Action? NavigateToAppointments { get; set; }
    public Action? NavigateToBilling { get; set; }
    public Action? NavigateToMedicalRecords { get; set; }

    public ICommand RefreshCommand { get; }
    public ICommand ViewAllPatientsCommand { get; }
    public ICommand ViewAllAppointmentsCommand { get; }
    public ICommand NavigateToBillingCommand { get; }
    public ICommand NavigateToMedicalRecordsCommand { get; }
    public ICommand AddPatientCommand { get; }
    public ICommand ScheduleAppointmentCommand { get; }
    public ICommand FilterCommand { get; }

    public DashboardViewModel(
        PatientService patientService,
        AppointmentService appointmentService,
        PaymentService paymentService,
        ToothService toothService)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
        _paymentService = paymentService;
        _toothService = toothService;

        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        ViewAllPatientsCommand = new RelayCommand(() => NavigateToPatients?.Invoke());
        ViewAllAppointmentsCommand = new RelayCommand(() => NavigateToAppointments?.Invoke());
        NavigateToBillingCommand = new RelayCommand(() => NavigateToBilling?.Invoke());
        NavigateToMedicalRecordsCommand = new RelayCommand(() => NavigateToMedicalRecords?.Invoke());
        AddPatientCommand = new RelayCommand(() => NavigateToPatients?.Invoke());
        ScheduleAppointmentCommand = new RelayCommand(() => NavigateToAppointments?.Invoke());
        FilterCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            TotalPatients = await _patientService.GetTotalCountAsync();
            NewPatientsThisMonth = await _patientService.GetNewPatientsThisMonthAsync();
            TodayAppointments = await _appointmentService.GetTodayCountAsync();
            UpcomingAppointments = await _appointmentService.GetUpcomingCountAsync();
            CompletedTreatments = await _appointmentService.GetCompletedCountAsync(FilterFrom, FilterTo);
            TodayRevenue = await _paymentService.GetTodayRevenueAsync();
            MonthRevenue = await _paymentService.GetMonthRevenueAsync();
            PendingBills = await _paymentService.GetPendingBillsAmountAsync();

            var todayAppts = await _appointmentService.GetTodayAsync();
            TodayAppointmentsList.Clear();
            foreach (var a in todayAppts)
                TodayAppointmentsList.Add(a);

            var recent = await _patientService.GetRecentAsync(5);
            RecentPatients.Clear();
            foreach (var p in recent)
                RecentPatients.Add(p);

            UpdateAlerts();
            await LoadChartsAsync();
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

    private void UpdateAlerts()
    {
        var alerts = new System.Collections.Generic.List<string>();
        if (PendingBills > 0)
            alerts.Add($"{PendingBills:N0} MAD in pending bills");
        if (TodayAppointments == 0 && DateTime.Now.Hour < 12)
            alerts.Add("No appointments scheduled for today");
        if (alerts.Count > 0)
        {
            AlertMessage = string.Join(" • ", alerts);
            HasAlerts = true;
        }
        else
        {
            AlertMessage = string.Empty;
            HasAlerts = false;
        }
    }

    private async Task LoadChartsAsync()
    {
        // Monthly Revenue (bar chart)
        var monthly = await _paymentService.GetMonthlyRevenueAsync(6);
        RevenueSeries.Clear();
        RevenueSeries.Add(new ColumnSeries<decimal>
        {
            Values = monthly.Select(x => x.Amount).ToArray(),
            Name = "Revenue (MAD)",
            Fill = new SolidColorPaint(SKColor.Parse("#6366F1"))
        });
        RevenueXAxes[0].Labels = monthly.Select(x => x.Month).ToArray();
        OnPropertyChanged(nameof(RevenueXAxes));

        // Appointments by Doctor (pie)
        var byDoctor = await _appointmentService.GetAppointmentsByDoctorAsync(6);
        DoctorPieSeries.Clear();
        var colors = new[] { "#6366F1", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#06B6D4" };
        for (int i = 0; i < byDoctor.Count; i++)
        {
            DoctorPieSeries.Add(new PieSeries<int>
            {
                Values = new[] { byDoctor[i].Count },
                Name = byDoctor[i].Doctor,
                Fill = new SolidColorPaint(SKColor.Parse(colors[i % colors.Length]))
            });
        }

        // Treatment/Condition breakdown (pie)
        var conditions = await _toothService.GetConditionBreakdownAsync();
        TreatmentPieSeries.Clear();
        var condColors = new[] { "#10B981", "#EF4444", "#3B82F6", "#F59E0B", "#8B5CF6", "#6B7280", "#06B6D4", "#EC4899" };
        for (int i = 0; i < conditions.Count; i++)
        {
            TreatmentPieSeries.Add(new PieSeries<int>
            {
                Values = new[] { conditions[i].Count },
                Name = conditions[i].Condition.ToString(),
                Fill = new SolidColorPaint(SKColor.Parse(condColors[i % condColors.Length]))
            });
        }

        // Demographics (pie)
        var (male, female, other) = await _patientService.GetGenderDemographicsAsync();
        DemographicsPieSeries.Clear();
        if (male > 0)
            DemographicsPieSeries.Add(new PieSeries<int> { Values = new[] { male }, Name = "Male", Fill = new SolidColorPaint(SKColor.Parse("#3B82F6")) });
        if (female > 0)
            DemographicsPieSeries.Add(new PieSeries<int> { Values = new[] { female }, Name = "Female", Fill = new SolidColorPaint(SKColor.Parse("#EC4899")) });
        if (other > 0)
            DemographicsPieSeries.Add(new PieSeries<int> { Values = new[] { other }, Name = "Other", Fill = new SolidColorPaint(SKColor.Parse("#6B7280")) });
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
