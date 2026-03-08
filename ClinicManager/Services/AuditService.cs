using System;
using System.Threading.Tasks;
using ClinicManager.Database;
using ClinicManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public static class AuditService
{
    public static async Task LogAsync(int userId, string action, string? details = null)
    {
        try
        {
            await using var db = new ClinicDbContext();
            db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                Timestamp = DateTime.UtcNow,
                Details = details
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Audit log failed: {ex.Message}");
        }
    }

    public static void Log(int userId, string action, string? details = null)
    {
        _ = LogAsync(userId, action, details);
    }

    public static class Actions
    {
        public const string Login = "Login";
        public const string Logout = "Logout";
        public const string CreatePatient = "CreatePatient";
        public const string EditPatient = "EditPatient";
        public const string DeletePatient = "DeletePatient";
        public const string CreateAppointment = "CreateAppointment";
        public const string EditAppointment = "EditAppointment";
        public const string DeleteAppointment = "DeleteAppointment";
        public const string CreatePayment = "CreatePayment";
        public const string EditPayment = "EditPayment";
        public const string DeletePayment = "DeletePayment";
        public const string EditMedicalRecord = "EditMedicalRecord";
        public const string DeleteMedicalRecord = "DeleteMedicalRecord";
        public const string CreateUser = "CreateUser";
        public const string DeactivateUser = "DeactivateUser";
        public const string ResetPassword = "ResetPassword";
        public const string ChangeRole = "ChangeRole";
    }
}
