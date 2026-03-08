using System;
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
    private static readonly string LicenseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClinicManager");

    private static readonly string LicensePath = Path.Combine(LicenseDir, ".license");

    // Shared secret used by both the app and the LicenseGenerator tool (required for key validation)
    private const string LicenseSecret = "ClinicManager-License-Secret-2026-XK9#mP2$vL";

    public bool IsLicensed()
    {
        var license = LoadLicense();
        if (license == null) return false;

        var currentMachine = HardwareFingerprint.Generate();
        if (license.MachineId != currentMachine) return false;

        return ValidateLicenseKey(license.LicenseKey, license.MachineId);
    }

    public string GetMachineId() => HardwareFingerprint.Generate();

    public bool ActivateLicense(string licenseKey, string licensedTo)
    {
        var machineId = HardwareFingerprint.Generate();
        if (!ValidateLicenseKey(licenseKey, machineId))
            return false;

        var license = new LicenseData
        {
            MachineId = machineId,
            LicenseKey = licenseKey,
            ActivationDate = DateTime.Now,
            LicensedTo = licensedTo
        };

        SaveLicense(license);
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

    private void SaveLicense(LicenseData license)
    {
        Directory.CreateDirectory(LicenseDir);
        var json = JsonSerializer.Serialize(license);
        var plainBytes = Encoding.UTF8.GetBytes(json);
        var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(LicensePath, encrypted);
        File.SetAttributes(LicensePath, FileAttributes.Hidden);
    }

    private LicenseData? LoadLicense()
    {
        if (!File.Exists(LicensePath)) return null;

        try
        {
            var encrypted = File.ReadAllBytes(LicensePath);
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
                SaveLicense(license);
            return license;
        }
        catch
        {
            return null;
        }
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
