using System;
using System.Security.Cryptography;
using System.Text;

namespace LicenseGenerator;

class Program
{
    private const string LicenseSecret = "ClinicManager-License-Secret-2026-XK9#mP2$vL";

    static void Main(string[] args)
    {
        Console.Title = "Clinic Manager - License Key Generator";

        while (true)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔═══════════════════════════════════════════════╗");
            Console.WriteLine("║   Clinic Manager - License Key Generator      ║");
            Console.WriteLine("╚═══════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            Console.Write("  Enter Machine ID (or 'q' to quit): ");
            var machineId = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(machineId) || machineId.Trim().ToLower() == "q")
                break;

            machineId = machineId.Trim().ToUpper();
            var licenseKey = GenerateLicenseKey(machineId);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  Machine ID:   {machineId}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  License Key:  {licenseKey}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("  Give this license key to the client.");
            Console.WriteLine();
            Console.WriteLine("  Press any key to generate another key...");
            Console.ReadKey(true);
        }
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
