using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace ClinicManager.Localization;

public class TranslationSource : INotifyPropertyChanged
{
    public static TranslationSource Instance { get; } = new();

    private readonly ResourceManager _resourceManager = Strings.ResourceManager;
    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    public string this[string key]
    {
        get
        {
            var result = _resourceManager.GetString(key, _currentCulture);
            return result ?? $"[{key}]";
        }
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture == value) return;
            _currentCulture = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }

    public void SetLanguage(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);
        CultureInfo.CurrentUICulture = culture;
        CurrentCulture = culture;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
