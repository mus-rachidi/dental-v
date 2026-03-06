using System;
using System.Security.Cryptography;
using System.Text;

string LicenseSecret = "ClinicManager-License-Secret-2026-XK9#mP2$vL";
string machineId = "ZYQSW82LGDURY0IOY8YZKAPEOGFXRDSL";

var payload = $"{machineId}:{LicenseSecret}";
var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
var b64 = Convert.ToBase64String(hash);
Console.WriteLine($"Base64: {b64}");

var encoded = b64
    .Replace("+", "X")
    .Replace("/", "Y")
    .Replace("=", "");
Console.WriteLine($"Encoded: {encoded}");

var trimmed = encoded[..25].ToUpper();
Console.WriteLine($"Trimmed: {trimmed}");

var key = $"{trimmed[..5]}-{trimmed[5..10]}-{trimmed[10..15]}-{trimmed[15..20]}-{trimmed[20..25]}";
Console.WriteLine($"License Key: {key}");
