using System.Windows;
using System.Windows.Controls;
using ClinicManager.Models;
using ClinicManager.ViewModels;

namespace ClinicManager.Views.Pages;

public partial class PatientsPage : UserControl
{
    private bool _suppressConditionChange;

    public PatientsPage()
    {
        InitializeComponent();
    }

    private void InfoTab_Click(object sender, RoutedEventArgs e)
    {
        InfoPanel.Visibility = Visibility.Visible;
        ChartPanel.Visibility = Visibility.Collapsed;
        XRayPanel.Visibility = Visibility.Collapsed;
    }

    private void ChartTab_Click(object sender, RoutedEventArgs e)
    {
        InfoPanel.Visibility = Visibility.Collapsed;
        ChartPanel.Visibility = Visibility.Visible;
        XRayPanel.Visibility = Visibility.Collapsed;
    }

    private void XRayTab_Click(object sender, RoutedEventArgs e)
    {
        InfoPanel.Visibility = Visibility.Collapsed;
        ChartPanel.Visibility = Visibility.Collapsed;
        XRayPanel.Visibility = Visibility.Visible;
    }

    private void Tooth_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ToothViewModel tooth && DataContext is PatientsViewModel vm)
        {
            vm.SelectTooth(tooth);
            _suppressConditionChange = true;
            ConditionCombo.SelectedIndex = (int)tooth.Condition;
            _suppressConditionChange = false;
        }
    }

    private void ConditionCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressConditionChange) return;
        if (DataContext is PatientsViewModel vm && vm.SelectedTooth != null && ConditionCombo.SelectedIndex >= 0)
        {
            vm.SelectedTooth.Condition = (ToothCondition)ConditionCombo.SelectedIndex;
        }
    }
}
