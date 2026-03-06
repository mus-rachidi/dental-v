using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace ClinicManager.Licensing;

public static class HardwareFingerprint
{
    public static string Generate()
    {
        var raw = new StringBuilder();
        raw.Append(GetWmiValue("Win32_Processor", "ProcessorId"));
        raw.Append(GetWmiValue("Win32_DiskDrive", "SerialNumber"));
        raw.Append(GetWmiValue("Win32_BaseBoard", "SerialNumber"));

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw.ToString()));
        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")[..32]
            .ToUpper();
    }

    private static string GetWmiValue(string wmiClass, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
            foreach (var obj in searcher.Get())
            {
                var val = obj[property]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(val))
                    return val;
            }
        }
        catch
        {
            // Fallback if WMI query fails
        }
        return Environment.MachineName;
    }
}
