using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ClinicManager.Helpers;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        DataPathHelper.GetClinicManagerDirectory(), "settings.json");

    private AppSettings? _cached;

    public async Task<AppSettings> LoadAsync()
    {
        if (_cached != null)
            return _cached;

        if (!File.Exists(SettingsPath))
        {
            _cached = new AppSettings();
            try { await SaveAsync(_cached); } catch { /* use defaults if save fails */ }
            return _cached;
        }

        try
        {
            var json = await File.ReadAllTextAsync(SettingsPath);
            _cached = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            _cached = new AppSettings();
        }

        return _cached;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(SettingsPath, json);
        _cached = settings;
    }

    public void ClearCache() => _cached = null;
}
