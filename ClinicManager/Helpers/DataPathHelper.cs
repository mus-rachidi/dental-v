using System;
using System.IO;

namespace ClinicManager.Helpers;

/// <summary>Returns a writable data directory. Avoids AppData when it causes access denied.</summary>
public static class DataPathHelper
{
    private static string? _cachedClinicManagerDir;

    /// <summary>Returns a writable ClinicManager data directory. Tries App dir, Documents, UserProfile, Temp, then AppData.</summary>
    public static string GetClinicManagerDirectory()
    {
        if (_cachedClinicManagerDir != null)
            return _cachedClinicManagerDir;

        var candidates = new[]
        {
            GetAppDir(),
            GetDocumentsDir(),
            GetUserProfileDir(),
            GetTempDir(),
            GetLocalAppDataDir()
        };

        foreach (var dir in candidates)
        {
            if (string.IsNullOrEmpty(dir)) continue;
            if (TryEnsureWritable(dir))
            {
                _cachedClinicManagerDir = dir;
                return dir;
            }
        }

        _cachedClinicManagerDir = GetLocalAppDataDir();
        return _cachedClinicManagerDir;
    }

    private static string? GetAppDir()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(baseDir)) return null;
            return Path.Combine(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "ClinicManager");
        }
        catch { return null; }
    }

    private static string GetDocumentsDir() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ClinicManager");

    private static string GetUserProfileDir() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ClinicManager");

    private static string GetTempDir() =>
        Path.Combine(Path.GetTempPath(), "ClinicManager");

    private static string GetLocalAppDataDir() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClinicManager");

    private static bool TryEnsureWritable(string dir)
    {
        try
        {
            Directory.CreateDirectory(dir);
            var testFile = Path.Combine(dir, ".write_test_" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
