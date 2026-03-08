using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClinicManager.ViewModels;

namespace ClinicManager.Views.Pages;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private void KpiPatients_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.NavigateToPatients?.Invoke();
    }

    private void KpiAppointments_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.NavigateToAppointments?.Invoke();
    }

    private void KpiBilling_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.NavigateToBilling?.Invoke();
    }

    private void KpiMedicalRecords_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.NavigateToMedicalRecords?.Invoke();
    }

    private void Appointment_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.NavigateToAppointments?.Invoke();
    }

    private void Patient_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
            vm.NavigateToPatients?.Invoke();
    }
}
