using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace ClinicManager.Localization;

public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocalizeExtension() { }

    public LocalizeExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return string.Empty;

        try
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = TranslationSource.Instance,
                Mode = BindingMode.OneWay,
                FallbackValue = $"[{Key}]"
            };
            return binding.ProvideValue(serviceProvider);
        }
        catch
        {
            try { return TranslationSource.Instance[Key]; }
            catch { return Key; }
        }
    }
}
