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

public class BillingViewModel : ViewModelBase, ILoadable
{
    private readonly PaymentService _paymentService;
    private readonly PatientService _patientService;
    private readonly ExportService _exportService;

    private Payment? _selectedPayment;
    private bool _isEditing;
    private bool _isLoading;
    private DateTime _filterFrom = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _filterTo = DateTime.Today;
    private decimal _totalAmount;

    private int _editId;
    private int _editPatientId;
    private decimal _editAmount;
    private DateTime _editDate = DateTime.Today;
    private PaymentMethod _editMethod = PaymentMethod.Cash;
    private PaymentStatus _editStatus = PaymentStatus.Completed;
    private string _editDescription = string.Empty;

    public Payment? SelectedPayment
    {
        get => _selectedPayment;
        set { SetProperty(ref _selectedPayment, value); OnPropertyChanged(nameof(HasSelection)); }
    }

    public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public bool HasSelection => SelectedPayment != null;
    public DateTime FilterFrom { get => _filterFrom; set => SetProperty(ref _filterFrom, value); }
    public DateTime FilterTo { get => _filterTo; set => SetProperty(ref _filterTo, value); }
    public decimal TotalAmount { get => _totalAmount; set => SetProperty(ref _totalAmount, value); }

    public int EditPatientId { get => _editPatientId; set => SetProperty(ref _editPatientId, value); }
    public decimal EditAmount { get => _editAmount; set => SetProperty(ref _editAmount, value); }
    public DateTime EditDate { get => _editDate; set => SetProperty(ref _editDate, value); }
    public PaymentMethod EditMethod { get => _editMethod; set => SetProperty(ref _editMethod, value); }
    public PaymentStatus EditStatus { get => _editStatus; set => SetProperty(ref _editStatus, value); }
    public string EditDescription { get => _editDescription; set => SetProperty(ref _editDescription, value); }

    public ObservableCollection<Payment> Payments { get; } = new();
    public ObservableCollection<Patient> AvailablePatients { get; } = new();

    public ICommand AddCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand RefreshCommand { get; }

    public BillingViewModel(PaymentService paymentService, PatientService patientService, ExportService exportService)
    {
        _paymentService = paymentService;
        _patientService = patientService;
        _exportService = exportService;

        AddCommand = new AsyncRelayCommand(StartAddAsync);
        EditCommand = new AsyncRelayCommand(StartEditAsync, () => HasSelection);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => HasSelection);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelEditCommand = new RelayCommand(() => IsEditing = false);
        FilterCommand = new AsyncRelayCommand(LoadAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        RefreshCommand = new AsyncRelayCommand(LoadAsync);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var list = await _paymentService.GetByDateRangeAsync(FilterFrom, FilterTo);
            Payments.Clear();
            foreach (var p in list) Payments.Add(p);
            TotalAmount = list.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
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
        EditAmount = 0;
        EditDate = DateTime.Today;
        EditMethod = PaymentMethod.Cash;
        EditStatus = PaymentStatus.Completed;
        EditDescription = string.Empty;
        IsEditing = true;
    }

    private async Task StartEditAsync()
    {
        if (SelectedPayment == null) return;
        await LoadPatientsAsync();

        _editId = SelectedPayment.Id;
        EditPatientId = SelectedPayment.PatientId;
        EditAmount = SelectedPayment.Amount;
        EditDate = SelectedPayment.Date;
        EditMethod = SelectedPayment.Method;
        EditStatus = SelectedPayment.Status;
        EditDescription = SelectedPayment.Description;
        IsEditing = true;
    }

    private async Task SaveAsync()
    {
        try
        {
            if (EditPatientId == 0)
            {
                Helpers.NotificationHelper.SelectPatientRequired();
                return;
            }

            var payment = new Payment
            {
                Id = _editId,
                PatientId = EditPatientId,
                Amount = EditAmount,
                Date = EditDate,
                Method = EditMethod,
                Status = EditStatus,
                Description = EditDescription
            };

            if (_editId == 0)
                await _paymentService.CreateAsync(payment);
            else
                await _paymentService.UpdateAsync(payment);

            IsEditing = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving payment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DeleteAsync()
    {
        if (SelectedPayment == null) return;
        var result = MessageBox.Show(Localization.Strings.ConfirmDelete, Localization.Strings.Confirm, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        await _paymentService.DeleteAsync(SelectedPayment.Id);
        SelectedPayment = null;
        await LoadAsync();
    }

    private async Task ExportAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel Files|*.xlsx",
            FileName = $"Payments_{DateTime.Now:yyyyMMdd}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var all = await _paymentService.GetByDateRangeAsync(FilterFrom, FilterTo);
                await _exportService.ExportPaymentsToExcelAsync(all, dialog.FileName);
                MessageBox.Show(Localization.Strings.ExportComplete, Localization.Strings.Success, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
