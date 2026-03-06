using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClinicManager.Database;

public class DatabaseBackupService
{
    private readonly ILogger<DatabaseBackupService> _logger;
    private const int MaxBackups = 10;

    public DatabaseBackupService(ILogger<DatabaseBackupService> logger)
    {
        _logger = logger;
    }

    public async Task<string> CreateBackupAsync(string? customPath = null)
    {
        var dbPath = ClinicDbContext.GetDatabasePath();
        if (!File.Exists(dbPath))
            throw new FileNotFoundException("Database file not found.", dbPath);

        var backupDir = customPath ?? Path.Combine(ClinicDbContext.GetDatabaseDirectory(), "Backups");
        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"clinic_backup_{timestamp}.db";
        var backupPath = Path.Combine(backupDir, backupFileName);

        await Task.Run(() => File.Copy(dbPath, backupPath, overwrite: true));

        _logger.LogInformation("Database backup created: {BackupPath}", backupPath);

        CleanOldBackups(backupDir);

        return backupPath;
    }

    public async Task RestoreBackupAsync(string backupPath)
    {
        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup file not found.", backupPath);

        var dbPath = ClinicDbContext.GetDatabasePath();
        var tempBackup = dbPath + ".temp";

        if (File.Exists(dbPath))
            File.Copy(dbPath, tempBackup, overwrite: true);

        try
        {
            await Task.Run(() => File.Copy(backupPath, dbPath, overwrite: true));
            _logger.LogInformation("Database restored from: {BackupPath}", backupPath);

            if (File.Exists(tempBackup))
                File.Delete(tempBackup);
        }
        catch
        {
            if (File.Exists(tempBackup))
                File.Move(tempBackup, dbPath, overwrite: true);
            throw;
        }
    }

    private void CleanOldBackups(string backupDir)
    {
        try
        {
            var backups = Directory.GetFiles(backupDir, "clinic_backup_*.db")
                                   .OrderByDescending(f => File.GetCreationTime(f))
                                   .Skip(MaxBackups)
                                   .ToList();

            foreach (var old in backups)
            {
                File.Delete(old);
                _logger.LogInformation("Deleted old backup: {Path}", old);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean old backups");
        }
    }

    public string[] GetAvailableBackups(string? customPath = null)
    {
        var backupDir = customPath ?? Path.Combine(ClinicDbContext.GetDatabaseDirectory(), "Backups");
        if (!Directory.Exists(backupDir))
            return Array.Empty<string>();

        return Directory.GetFiles(backupDir, "clinic_backup_*.db")
                        .OrderByDescending(f => File.GetCreationTime(f))
                        .ToArray();
    }
}
