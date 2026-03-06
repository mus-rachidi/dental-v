using System.Windows;
using ClinicManager.Licensing;

namespace ClinicManager.Views.Dialogs;

public partial class LicenseDialog : Window
{
    private readonly LicenseManager _licenseManager;

    public bool IsActivated { get; private set; }

    public LicenseDialog(LicenseManager licenseManager)
    {
        InitializeComponent();
        _licenseManager = licenseManager;
        MachineIdText.Text = licenseManager.GetMachineId();
    }

    private void CopyMachineId_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(MachineIdText.Text);
        CopyButton.Content = "Copied!";
    }

    private void ActivateButton_Click(object sender, RoutedEventArgs e)
    {
        var key = LicenseKeyInput.Text.Trim();
        var name = LicensedToInput.Text.Trim();

        if (string.IsNullOrEmpty(key))
        {
            ShowError("Please enter a license key.");
            return;
        }

        if (string.IsNullOrEmpty(name))
        {
            ShowError("Please enter a name.");
            return;
        }

        if (_licenseManager.ActivateLicense(key, name))
        {
            IsActivated = true;
            MessageBox.Show("License activated successfully!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        else
        {
            ShowError("Invalid license key. Please check and try again.");
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
