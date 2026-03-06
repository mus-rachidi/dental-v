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

    private void Tooth_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ToothViewModel tooth && DataContext is PatientsViewModel vm)
        {
            vm.SelectedTooth = tooth;
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
