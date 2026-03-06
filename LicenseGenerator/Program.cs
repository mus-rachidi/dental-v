using System;
using System.Security.Cryptography;
using System.Text;

namespace LicenseGenerator;

/// <summary>
/// Standalone console tool for generating license keys.
/// Run this on your machine (the vendor), NOT on the client machine.
/// The client provides their Machine ID (displayed on first launch),
/// and this tool outputs the corresponding license key.
/// </summary>
class Program
{
    private const string LicenseSecret = "ClinicManager-License-Secret-2026-XK9#mP2$vL";

    static void Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  Clinic Manager - License Key Generator");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        string? machineId;

        if (args.Length > 0)
        {
            machineId = args[0];
        }
        else
        {
            Console.Write("Enter Machine ID: ");
            machineId = Console.ReadLine();
        }

        if (string.IsNullOrWhiteSpace(machineId))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Machine ID is required.");
            Console.ResetColor();
            return;
        }

        machineId = machineId.Trim().ToUpper();
        var licenseKey = GenerateLicenseKey(machineId);

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Machine ID:  {machineId}");
        Console.WriteLine($"License Key: {licenseKey}");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("Provide this license key to the client.");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static string GenerateLicenseKey(string machineId)
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
}
