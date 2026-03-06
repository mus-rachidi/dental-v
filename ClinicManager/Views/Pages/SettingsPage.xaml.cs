using System.Windows.Controls;
using ClinicManager.ViewModels;

namespace ClinicManager.Views.Pages;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private void LanguageCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && LanguageCombo.SelectedIndex >= 0)
        {
            var lang = LanguageCombo.SelectedIndex == 0 ? "en" : "fr";
            vm.ApplyLanguage(lang);
        }
    }

    private void ThemeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && ThemeCombo.SelectedIndex >= 0)
        {
            var theme = ThemeCombo.SelectedIndex == 0 ? "Light" : "Dark";
            vm.ApplyThemeFromUI(theme);
        }
    }
}
