using System.Windows.Input;
using ClinicManager.Helpers;
using ClinicManager.Services;

namespace ClinicManager.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel;
    private string _currentSection = "Dashboard";

    public DashboardViewModel DashboardVM { get; }
    public PatientsViewModel PatientsVM { get; }
    public AppointmentsViewModel AppointmentsVM { get; }
    public StaffViewModel StaffVM { get; }
    public BillingViewModel BillingVM { get; }
    public InventoryViewModel InventoryVM { get; }
    public MedicalRecordsViewModel MedicalRecordsVM { get; }
    public ReportsViewModel ReportsVM { get; }
    public SettingsViewModel SettingsVM { get; }
    public UsersManagementViewModel UsersVM { get; }

    public bool CanManageUsers => App.SessionService?.HasPermission(Models.Permission.ManageUsers) ?? false;

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    public string CurrentSection
    {
        get => _currentSection;
        set => SetProperty(ref _currentSection, value);
    }

    public ICommand NavigateCommand { get; }

    public MainViewModel(
        PatientService patientService,
        AppointmentService appointmentService,
        PaymentService paymentService,
        MedicalRecordService medicalRecordService,
        ToothService toothService,
        SettingsService settingsService,
        ExportService exportService,
        UserService userService,
        AuthService authService,
        Licensing.LicenseManager licenseManager,
        Database.DatabaseBackupService backupService)
    {
        DashboardVM = new DashboardViewModel(patientService, appointmentService, paymentService, toothService);
        PatientsVM = new PatientsViewModel(patientService, exportService, settingsService);
        AppointmentsVM = new AppointmentsViewModel(appointmentService, patientService);
        StaffVM = new StaffViewModel();
        BillingVM = new BillingViewModel(paymentService, patientService, exportService);
        InventoryVM = new InventoryViewModel();
        MedicalRecordsVM = new MedicalRecordsViewModel(medicalRecordService, patientService);
        ReportsVM = new ReportsViewModel();
        SettingsVM = new SettingsViewModel(settingsService, licenseManager, backupService);
        UsersVM = new UsersManagementViewModel(userService, authService);

        _currentViewModel = DashboardVM;

        NavigateCommand = new RelayCommand(Navigate);

        DashboardVM.NavigateToPatients = () => Navigate("Patients");
        DashboardVM.NavigateToAppointments = () => Navigate("Appointments");
        DashboardVM.NavigateToBilling = () => Navigate("Billing");
        DashboardVM.NavigateToMedicalRecords = () => Navigate("MedicalRecords");
    }

    private void Navigate(object parameter)
    {
        var section = parameter?.ToString() ?? "Dashboard";
        CurrentSection = section;

        CurrentViewModel = section switch
        {
            "Dashboard" => DashboardVM,
            "Patients" => PatientsVM,
            "Appointments" => AppointmentsVM,
            "Staff" => StaffVM,
            "Billing" => BillingVM,
            "Inventory" => InventoryVM,
            "MedicalRecords" => MedicalRecordsVM,
            "Reports" => ReportsVM,
            "Settings" => SettingsVM,
            "Users" => UsersVM,
            _ => DashboardVM
        };

        if (CurrentViewModel is ILoadable loadable)
            _ = loadable.LoadAsync();
    }

    public async void Initialize()
    {
        await DashboardVM.LoadAsync();
        await SettingsVM.LoadAsync(); // Load language/theme for top bar
    }
}

public interface ILoadable
{
    System.Threading.Tasks.Task LoadAsync();
}
