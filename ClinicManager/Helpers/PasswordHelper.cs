using System;
using System.Security.Cryptography;

namespace ClinicManager.Helpers;

/// <summary>Secure password hashing and verification using PBKDF2.</summary>
public static class PasswordHelper
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;
    private const char Delimiter = ':';

    /// <summary>Hashes a password for storage. Returns format: base64(salt):base64(hash).</summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Convert.ToBase64String(salt)}{Delimiter}{Convert.ToBase64String(hash)}";
    }

    /// <summary>Verifies a password against a stored hash. Supports PBKDF2 (salt:hash) and legacy BCrypt ($2a$...).</summary>
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        // Legacy BCrypt format (e.g. from old DB)
        if (storedHash.StartsWith("$2a$", StringComparison.Ordinal) || storedHash.StartsWith("$2b$", StringComparison.Ordinal))
        {
            try
            {
                return global::BCrypt.Net.BCrypt.Verify(password, storedHash);
            }
            catch
            {
                return false;
            }
        }

        // PBKDF2 format: base64(salt):base64(hash)
        try
        {
            var parts = storedHash.Split(Delimiter);
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var expectedHash = Convert.FromBase64String(parts[1]);
            if (salt.Length != SaltSize || expectedHash.Length != HashSize)
                return false;

            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch
        {
            return false;
        }
    }
}
