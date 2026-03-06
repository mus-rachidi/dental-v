using System.ComponentModel;
using System.Windows;
using ClinicManager.ViewModels;

namespace ClinicManager.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Initialize();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        var settings = ((MainViewModel)DataContext).SettingsVM;
        if (settings.MinimizeToTray)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
        }
        else
        {
            base.OnClosing(e);
        }
    }
}
