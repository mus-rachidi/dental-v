using System.Windows;
using System.Windows.Controls;
using ClinicManager.ViewModels;

namespace ClinicManager.Views.Pages;

public partial class UsersPage : UserControl
{
    public UsersPage()
    {
        InitializeComponent();
    }

    private void EditPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is UsersManagementViewModel vm && sender is PasswordBox pb)
            vm.EditPassword = pb.Password;
    }
}
