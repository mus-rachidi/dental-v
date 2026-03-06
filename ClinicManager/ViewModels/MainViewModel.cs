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
    public BillingViewModel BillingVM { get; }
    public MedicalRecordsViewModel MedicalRecordsVM { get; }
    public SettingsViewModel SettingsVM { get; }

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
        SettingsService settingsService,
        ExportService exportService,
        Licensing.LicenseManager licenseManager,
        Database.DatabaseBackupService backupService)
    {
        DashboardVM = new DashboardViewModel(patientService, appointmentService, paymentService);
        PatientsVM = new PatientsViewModel(patientService, exportService, settingsService);
        AppointmentsVM = new AppointmentsViewModel(appointmentService, patientService);
        BillingVM = new BillingViewModel(paymentService, patientService, exportService);
        MedicalRecordsVM = new MedicalRecordsViewModel(medicalRecordService, patientService);
        SettingsVM = new SettingsViewModel(settingsService, licenseManager, backupService);

        _currentViewModel = DashboardVM;

        NavigateCommand = new RelayCommand(Navigate);

        DashboardVM.NavigateToPatients = () => Navigate("Patients");
        DashboardVM.NavigateToAppointments = () => Navigate("Appointments");
    }

    private void Navigate(object? parameter)
    {
        var section = parameter?.ToString() ?? "Dashboard";
        CurrentSection = section;

        CurrentViewModel = section switch
        {
            "Dashboard" => DashboardVM,
            "Patients" => PatientsVM,
            "Appointments" => AppointmentsVM,
            "Billing" => BillingVM,
            "MedicalRecords" => MedicalRecordsVM,
            "Settings" => SettingsVM,
            _ => DashboardVM
        };

        if (CurrentViewModel is ILoadable loadable)
            _ = loadable.LoadAsync();
    }

    public async void Initialize()
    {
        await DashboardVM.LoadAsync();
    }
}

public interface ILoadable
{
    System.Threading.Tasks.Task LoadAsync();
}
