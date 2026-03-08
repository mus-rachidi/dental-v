using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClinicManager.Database;
using ClinicManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class AuthResult
{
    public bool Success { get; set; }
    public User? User { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
}

public class AuthService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;
    private const int MinPasswordLength = 8;
    private static readonly Regex PasswordStrengthRegex = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        RegexOptions.Compiled);

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new AuthResult { Success = false, Message = "Username and password are required." };

        await using var db = new ClinicDbContext();
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));

        if (user == null)
            return new AuthResult { Success = false, Message = "Invalid username or password." };

        if (user.Status == UserStatus.Inactive)
            return new AuthResult { Success = false, Message = "This account has been deactivated." };

        if (user.Status == UserStatus.Locked || (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow))
        {
            var remaining = (user.LockoutEnd!.Value - DateTime.UtcNow).TotalMinutes;
            return new AuthResult
            {
                Success = false,
                Message = $"Account is locked. Try again in {Math.Ceiling(remaining)} minutes."
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.Status = UserStatus.Locked;
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
            }
            await db.SaveChangesAsync();
            return new AuthResult { Success = false, Message = "Invalid username or password." };
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.Status = UserStatus.Active;
        user.LastLogin = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new AuthResult
        {
            Success = true,
            User = user,
            MustChangePassword = user.MustChangePassword
        };
    }

    public async Task<(bool Success, string Message)> CreateUserAsync(string username, string password, UserRole role)
    {
        var validation = ValidatePassword(password);
        if (!validation.Success)
            return validation;

        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return (false, "Username must be at least 3 characters.");

        await using var db = new ClinicDbContext();
        var exists = await db.Users.AnyAsync(u =>
            u.Username.Equals(username.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exists)
            return (false, "Username already exists.");

        var user = new User
        {
            Username = username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12)),
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (true, "User created successfully.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPassword)
    {
        var validation = ValidatePassword(newPassword);
        if (!validation.Success)
            return validation;

        await using var db = new ClinicDbContext();
        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.Status = UserStatus.Active;
        user.MustChangePassword = false;
        await db.SaveChangesAsync();
        return (true, "Password reset successfully.");
    }

    public async Task<(bool Success, string Message)> ChangeRoleAsync(int userId, UserRole newRole)
    {
        await using var db = new ClinicDbContext();
        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found.");

        user.Role = newRole;
        await db.SaveChangesAsync();
        return (true, "Role updated successfully.");
    }

    public async Task<(bool Success, string Message)> SetUserStatusAsync(int userId, UserStatus status)
    {
        await using var db = new ClinicDbContext();
        var user = await db.Users.FindAsync(userId);
        if (user == null)
            return (false, "User not found.");

        user.Status = status;
        if (status == UserStatus.Active)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }
        await db.SaveChangesAsync();
        return (true, "User status updated.");

    }

    public static (bool Success, string Message) ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "Password is required.");

        if (password.Length < MinPasswordLength)
            return (false, $"Password must be at least {MinPasswordLength} characters.");

        if (!PasswordStrengthRegex.IsMatch(password))
            return (false, "Password must contain uppercase, lowercase, number and special character.");

        return (true, string.Empty);
    }

    public async Task EnsureAdminExistsAsync()
    {
        await using var db = new ClinicDbContext();
        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        var admin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", BCrypt.Net.BCrypt.GenerateSalt(12)),
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            MustChangePassword = true
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
