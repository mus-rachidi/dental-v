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
    private const int MinPasswordLength = 8;
    private const string DefaultUsername = "admin";
    private const string DefaultPassword = "Admin@123";

    /// <summary>Default demo credentials for development/testing. Use "Use demo credentials" on login.</summary>
    public static string DemoUsername => DefaultUsername;
    public static string DemoPassword => DefaultPassword;

    // Lazy init to avoid TypeInitializationException from static Regex
    private static Regex? _passwordStrengthRegex;
    private static Regex PasswordStrengthRegex =>
        _passwordStrengthRegex ??= new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return new AuthResult { Success = false, Message = "Username and password are required." };

        try
        {
            var trimmed = username.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return new AuthResult { Success = false, Message = "Username and password are required." };

            await using var db = new ClinicDbContext();
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == trimmed.ToLower());

            if (user == null)
            {
                // If no users exist and they're trying default credentials, create admin and retry
                var anyUsers = await db.Users.AnyAsync();
                if (!anyUsers && string.Equals(trimmed, DefaultUsername, StringComparison.OrdinalIgnoreCase) && password?.Trim() == DefaultPassword)
                {
                    await EnsureAdminExistsAsync();
                    return await LoginAsync(username, password);
                }
                return new AuthResult { Success = false, Message = "Invalid username or password." };
            }

            if (user.Status == UserStatus.Inactive)
                return new AuthResult { Success = false, Message = "This account has been deactivated." };

            if (user.Status == UserStatus.Locked || user.LockoutEnd.HasValue || user.FailedLoginAttempts > 0)
            {
                await using var dbUnlock = new ClinicDbContext();
                var userToUnlock = await dbUnlock.Users.FindAsync(user.Id);
                if (userToUnlock != null)
                {
                    userToUnlock.FailedLoginAttempts = 0;
                    userToUnlock.LockoutEnd = null;
                    if (userToUnlock.Status == UserStatus.Locked)
                        userToUnlock.Status = UserStatus.Active;
                    await dbUnlock.SaveChangesAsync();
                    user = userToUnlock;
                }
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
                return new AuthResult { Success = false, Message = "Invalid account. Please contact administrator." };

            bool passwordValid = PasswordHelper.VerifyPassword(password, user.PasswordHash);
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Auth] Verify: user={user.Username}, valid={passwordValid}, hashLen={user.PasswordHash?.Length ?? 0}");
#endif

            if (!passwordValid)
                return new AuthResult { Success = false, Message = "Password is incorrect." };

            await using var dbSave = new ClinicDbContext();
            var userToSave = await dbSave.Users.FindAsync(user.Id);
            if (userToSave != null)
            {
                userToSave.FailedLoginAttempts = 0;
                userToSave.LockoutEnd = null;
                userToSave.Status = UserStatus.Active;
                userToSave.LastLogin = DateTime.UtcNow;
                await dbSave.SaveChangesAsync();
                user = userToSave;
            }

            return new AuthResult
            {
                Success = true,
                User = user,
                MustChangePassword = user.MustChangePassword
            };
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            return new AuthResult { Success = false, Message = "Database error. Try again or restart the app." };
        }
        catch (TypeInitializationException ex)
        {
            var full = GetExceptionDetails(ex);
            return new AuthResult { Success = false, Message = $"Initialization error: {full}" };
        }
        catch (Exception ex)
        {
            var full = GetExceptionDetails(ex);
            return new AuthResult { Success = false, Message = $"Error: {full}" };
        }
    }

    public async Task<(bool Success, string Message)> CreateUserAsync(string username, string password, UserRole role)
    {
        var validation = ValidatePassword(password);
        if (!validation.Success)
            return validation;

        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return (false, "Username must be at least 3 characters.");

        var trimmed = username.Trim();
        await using var db = new ClinicDbContext();
        var exists = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Username.ToLower() == trimmed.ToLower());
        if (exists)
            return (false, "Username already exists.");

        var user = new User
        {
            Username = trimmed,
            PasswordHash = PasswordHelper.HashPassword(password),
            Role = role,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            MustChangePassword = true
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

        user.PasswordHash = PasswordHelper.HashPassword(newPassword);
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

    private static string GetExceptionDetails(Exception ex)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(ex.Message);
        var inner = ex.InnerException;
        while (inner != null)
        {
            sb.Append(" | Inner: ").Append(inner.GetType().Name).Append(": ").Append(inner.Message);
            inner = inner.InnerException;
        }
        sb.Append(" | Stack: ").Append(ex.StackTrace?.Split('\n')[0] ?? "N/A");
        return sb.ToString();
    }

    /// <summary>Creates admin (admin/Admin@123) only when no admin exists. Does NOT reset existing admin password - user's chosen password persists.</summary>
    public async Task EnsureAdminExistsAsync()
    {
        await using var db = new ClinicDbContext();
        if (await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return; // Admin exists - keep their password, do not reset

        var hash = PasswordHelper.HashPassword(DefaultPassword);
        var admin = new User
        {
            Username = DefaultUsername,
            PasswordHash = hash,
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            MustChangePassword = true
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    /// <summary>True only on first-time setup: admin has not yet changed password. Hide demo credentials after user sets new password.</summary>
    public async Task<bool> ShouldShowDemoCredentialsAsync()
    {
        try
        {
            await using var db = new ClinicDbContext();
            var admin = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username.ToLower() == DefaultUsername);
            if (admin == null) return true; // No admin yet - will be created on first login
            return admin.MustChangePassword; // Show only when password change is required (first login)
        }
        catch { return false; }
    }

    /// <summary>Resets the "admin" user password to Admin@123 so the default credential always works.</summary>
    public async Task ResetDefaultAdminPasswordAsync()
    {
        var hash = PasswordHelper.HashPassword(DefaultPassword);

        await using var db = new ClinicDbContext();
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == DefaultUsername);
        if (adminUser == null)
        {
            var newAdmin = new User
            {
                Username = DefaultUsername,
                PasswordHash = hash,
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                MustChangePassword = false
            };
            db.Users.Add(newAdmin);
            await db.SaveChangesAsync();
            return;
        }

        adminUser.PasswordHash = hash;
        adminUser.FailedLoginAttempts = 0;
        adminUser.LockoutEnd = null;
        adminUser.Status = UserStatus.Active;
        adminUser.MustChangePassword = false;
        await db.SaveChangesAsync();
    }
}
