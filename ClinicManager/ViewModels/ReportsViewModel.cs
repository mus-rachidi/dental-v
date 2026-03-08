using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ClinicManager.Helpers;
using ClinicManager.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using SkiaSharp;

namespace ClinicManager.ViewModels;

public class ReportsViewModel : ViewModelBase, ILoadable
{
    private readonly PaymentService _paymentService;
    private readonly PatientService _patientService;
    private readonly AppointmentService _appointmentService;
    private readonly ExportService _exportService;
    private readonly SettingsService _settingsService;

    private bool _isLoading;
    private DateTime _filterFrom = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _filterTo = DateTime.Today;

    private int _totalPatients;
    private int _newPatientsInPeriod;
    private int _totalAppointments;
    private int _completedAppointments;
    private decimal _totalRevenue;
    private decimal _pendingBills;
    private int _lowStockCount;

    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public DateTime FilterFrom { get => _filterFrom; set => SetProperty(ref _filterFrom, value); }
    public DateTime FilterTo { get => _filterTo; set => SetProperty(ref _filterTo, value); }

    public int TotalPatients { get => _totalPatients; set => SetProperty(ref _totalPatients, value); }
    public int NewPatientsInPeriod { get => _newPatientsInPeriod; set => SetProperty(ref _newPatientsInPeriod, value); }
    public int TotalAppointments { get => _totalAppointments; set => SetProperty(ref _totalAppointments, value); }
    public int CompletedAppointments { get => _completedAppointments; set => SetProperty(ref _completedAppointments, value); }
    public decimal TotalRevenue { get => _totalRevenue; set => SetProperty(ref _totalRevenue, value); }
    public decimal PendingBills { get => _pendingBills; set => SetProperty(ref _pendingBills, value); }
    public int LowStockCount { get => _lowStockCount; set => SetProperty(ref _lowStockCount, value); }

    public ObservableCollection<ISeries> RevenueSeries { get; } = new();
    public ObservableCollection<ISeries> AppointmentsByDoctorSeries { get; } = new();
    public Axis[] RevenueXAxes { get; } = { new Axis() };

    public ICommand RefreshCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportPdfCommand { get; }

    public ReportsViewModel(
        PaymentService paymentService,
        PatientService patientService,
        AppointmentService appointmentService,
        ExportService exportService,
        SettingsService settingsService,
        InventoryService inventoryService)
    {
        _paymentService = paymentService;
        _patientService = patientService;
        _appointmentService = appointmentService;
        _exportService = exportService;
        _settingsService = settingsService;

        RefreshCommand = new AsyncRelayCommand(LoadAsync);
        ExportExcelCommand = new AsyncRelayCommand(ExportExcelAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);

        _ = LoadLowStockCountAsync(inventoryService);
    }

    private async Task LoadLowStockCountAsync(InventoryService inventoryService)
    {
        try
        {
            var low = await inventoryService.GetLowStockAsync();
            LowStockCount = low.Count;
        }
        catch { LowStockCount = 0; }
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            TotalPatients = await _patientService.GetTotalCountAsync();
            var firstDay = new DateTime(FilterFrom.Year, FilterFrom.Month, 1);
            var lastDay = FilterTo.Date;
            NewPatientsInPeriod = await _patientService.GetTotalCountAsync(); // simplified
            var allPatients = await _patientService.GetAllAsync();
            NewPatientsInPeriod = allPatients.Count(p => p.CreatedAt >= firstDay && p.CreatedAt.Date <= lastDay);

            var appointments = await _appointmentService.GetByDateRangeAsync(FilterFrom, FilterTo);
            TotalAppointments = appointments.Count;
            CompletedAppointments = appointments.Count(a => a.Status == Models.AppointmentStatus.Completed);

            var payments = await _paymentService.GetByDateRangeAsync(FilterFrom, FilterTo);
            TotalRevenue = payments.Where(p => p.Status == Models.PaymentStatus.Completed).Sum(p => p.Amount);
            PendingBills = await _paymentService.GetPendingBillsAmountAsync();

            await LoadChartsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading reports: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadChartsAsync()
    {
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

        var byDoctor = await _appointmentService.GetAppointmentsByDoctorAsync(6);
        AppointmentsByDoctorSeries.Clear();
        var colors = new[] { "#6366F1", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#06B6D4" };
        for (int i = 0; i < byDoctor.Count; i++)
        {
            AppointmentsByDoctorSeries.Add(new PieSeries<int>
            {
                Values = new[] { byDoctor[i].Count },
                Name = byDoctor[i].Doctor,
                Fill = new SolidColorPaint(SKColor.Parse(colors[i % colors.Length]))
            });
        }
    }

    private async Task ExportExcelAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"ClinicReport_{DateTime.Now:yyyyMMdd}"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var payments = await _paymentService.GetByDateRangeAsync(FilterFrom, FilterTo);
            await _exportService.ExportPaymentsToExcelAsync(payments, dialog.FileName);
            MessageBox.Show("Report exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportPdfAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF Files|*.pdf",
            FileName = $"ClinicReport_{DateTime.Now:yyyyMMdd}"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var payments = await _paymentService.GetByDateRangeAsync(FilterFrom, FilterTo);
            var settings = await _settingsService.LoadAsync();
            await _exportService.ExportPaymentsToPdfAsync(payments, dialog.FileName, settings.ClinicName);
            MessageBox.Show("Report exported successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
