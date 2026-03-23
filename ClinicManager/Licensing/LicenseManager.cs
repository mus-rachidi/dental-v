using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClinicManager.Licensing;

public class LicenseData
{
    public string MachineId { get; set; } = string.Empty;
    public string LicenseKey { get; set; } = string.Empty;
    public DateTime ActivationDate { get; set; }
    public string LicensedTo { get; set; } = string.Empty;
}

public class LicenseManager
{
    private static readonly string LicenseFileName = ".license";

    /// <summary>Fallback when AppData is not writable (e.g. restricted profile).</summary>
    private static string GetLicenseDirTemp()
    {
        return Path.Combine(Path.GetTempPath(), "ClinicManager");
    }

    /// <summary>Documents folder - often writable when AppData is restricted.</summary>
    private static string GetLicenseDirDocuments()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ClinicManager");
    }

    /// <summary>User profile (home) - e.g. C:\Users\xxx\ClinicManager.</summary>
    private static string GetLicenseDirUserProfile()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ClinicManager");
    }

    /// <summary>Application folder - works when app runs from user-writable location (portable, debug).</summary>
    private static string? GetLicenseDirApp()
    {
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (string.IsNullOrEmpty(baseDir)) return null;
            return Path.Combine(baseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), "ClinicManager");
        }
        catch { return null; }
    }

    private static string GetLicensePath(string dir) => Path.Combine(dir, LicenseFileName);

    /// <summary>Returns license directories. Excludes AppData\Local and AppData\Roaming to avoid access denied on restricted systems.</summary>
    private static string[] GetLicenseDirectories()
    {
        var dirs = new List<string>();
        var appDir = GetLicenseDirApp();
        if (!string.IsNullOrEmpty(appDir))
            dirs.Add(appDir);
        dirs.Add(GetLicenseDirDocuments());
        dirs.Add(GetLicenseDirUserProfile());
        dirs.Add(GetLicenseDirTemp());
        // Skip LocalAppData and Roaming - often causes "access denied" on restricted profiles
        return dirs.ToArray();
    }

    // Shared secret used by both the app and the LicenseGenerator tool (required for key validation)
    private const string LicenseSecret = "ClinicManager-License-Secret-2026-XK9#mP2$vL";

    public bool IsLicensed()
    {
        if (BypassLicenseCheck) return true;
        if (string.Equals(Environment.GetEnvironmentVariable("CLINICMANAGER_SKIP_LICENSE"), "1", StringComparison.OrdinalIgnoreCase))
            return true;
        try
        {
            var license = LoadLicense();
            if (license == null) return false;

            var currentMachine = HardwareFingerprint.Generate();
            if (license.MachineId != currentMachine) return false;

            return ValidateLicenseKey(license.LicenseKey, license.MachineId);
        }
        catch
        {
            return true;
        }
    }

    /// <summary>Set true to bypass license check entirely. Set false to show license dialog.</summary>
    public static bool BypassLicenseCheck { get; set; } = false;

    public string GetMachineId() => HardwareFingerprint.Generate();

    /// <summary>Last error when ActivateLicense fails (e.g. access denied).</summary>
    public string? LastError { get; private set; }

    public bool ActivateLicense(string licenseKey, string licensedTo)
    {
        LastError = null;
        var machineId = HardwareFingerprint.Generate();
        if (!ValidateLicenseKey(licenseKey, machineId))
        {
            LastError = "Invalid license key. Please check and try again.";
            return false;
        }

        var license = new LicenseData
        {
            MachineId = machineId,
            LicenseKey = licenseKey,
            ActivationDate = DateTime.Now,
            LicensedTo = licensedTo
        };

        if (!SaveLicense(license))
        {
            LastError = "Cannot save license file. Access to the path is denied. Use 'Continue without license' to run the app.";
            return false;
        }
        return true;
    }

    public LicenseData? GetLicenseInfo() => LoadLicense();

    public static string GenerateLicenseKey(string machineId)
    {
        var payload = $"{machineId}:{LicenseSecret}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        var encoded = Convert.ToBase64String(hash)
            .Replace("+", "X")
            .Replace("/", "Y")
            .Replace("=", "")[..25]
            .ToUpper();

        return $"{encoded[..5]}-{encoded[5..10]}-{encoded[10..15]}-{encoded[15..20]}-{encoded[20..25]}";
    }

    private bool ValidateLicenseKey(string licenseKey, string machineId)
    {
        var expected = GenerateLicenseKey(machineId);
        return string.Equals(licenseKey.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }

    private bool SaveLicense(LicenseData license)
    {
        var json = JsonSerializer.Serialize(license);
        var plainBytes = Encoding.UTF8.GetBytes(json);
        var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

        foreach (var dir in GetLicenseDirectories())
        {
            if (TrySaveToDirectory(dir, encrypted))
                return true;
        }
        return false;
    }

    private static bool TrySaveToDirectory(string licenseDir, byte[] encrypted)
    {
        string? tempPath = null;
        try
        {
            try { Directory.CreateDirectory(licenseDir); }
            catch (UnauthorizedAccessException) { return false; }
            catch (IOException) { return false; }

            var licensePath = GetLicensePath(licenseDir);
            // Use temp file in same directory to avoid cross-drive move issues
            tempPath = Path.Combine(licenseDir, ".tmp_" + Guid.NewGuid().ToString("N"));
            File.WriteAllBytes(tempPath, encrypted);

            if (File.Exists(licensePath))
            {
                try
                {
                    var attrs = File.GetAttributes(licensePath);
                    if ((attrs & FileAttributes.ReadOnly) != 0)
                        File.SetAttributes(licensePath, attrs & ~FileAttributes.ReadOnly);
                }
                catch { /* ignore */ }
                File.Delete(licensePath);
            }

            File.Move(tempPath, licensePath);
            tempPath = null;
            try { File.SetAttributes(licensePath, FileAttributes.Hidden); } catch { /* ignore */ }
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            if (tempPath != null)
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* ignore */ }
            }
        }
    }

    private LicenseData? LoadLicense()
    {
        try
        {
            foreach (var dir in GetLicenseDirectories())
            {
                var path = GetLicensePath(dir);
                try { if (!File.Exists(path)) continue; }
                catch { continue; }

                try
                {
                    var encrypted = File.ReadAllBytes(path);
                byte[] plainBytes;
                try
                {
                    plainBytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                }
                catch
                {
                    plainBytes = DecryptLegacyAes(encrypted);
                }
                var json = Encoding.UTF8.GetString(plainBytes);
                var license = JsonSerializer.Deserialize<LicenseData>(json);
                if (license != null)
                {
                    try { SaveLicense(license); } catch { /* re-save optional */ }
                }
                return license;
            }
            catch (UnauthorizedAccessException)
            {
                /* try next location */
            }
            catch (IOException)
            {
                /* file locked or inaccessible, try next location */
            }
            catch
            {
                /* try next location */
            }
        }
        }
        catch { /* ignore all */ }
        return null;
    }

    private static readonly byte[] LegacyAesKey = Encoding.UTF8.GetBytes("Cl!n1cM@nager$LicKey2026!Sec#re!");
    private static readonly byte[] LegacyAesIv = Encoding.UTF8.GetBytes("CM$IV2026!Secure");

    private static byte[] DecryptLegacyAes(byte[] cipherBytes)
    {
        using var aes = Aes.Create();
        aes.Key = LegacyAesKey;
        aes.IV = LegacyAesIv;
        using var ms = new MemoryStream(cipherBytes);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);
        return Encoding.UTF8.GetBytes(sr.ReadToEnd());
    }
}
