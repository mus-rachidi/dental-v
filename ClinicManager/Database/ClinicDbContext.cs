using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Models;

namespace ClinicManager.Database;

public class ClinicDbContext : DbContext
{
    private static readonly string DbDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClinicManager");

    private static readonly string DbPath = Path.Combine(DbDirectory, "clinic.db");

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<ToothRecord> ToothRecords => Set<ToothRecord>();
    public DbSet<XRayRecord> XRayRecords => Set<XRayRecord>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Directory.CreateDirectory(DbDirectory);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DbPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();

        optionsBuilder.UseSqlite(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(e => e.FullName);
            entity.HasIndex(e => e.Phone);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => new { e.Date, e.DoctorName });
            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.Appointments)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
            // SQLite doesn't support TimeSpan in ORDER BY - store as ticks (long) for sortable queries
            entity.Property(e => e.Time)
                  .HasConversion(
                      v => v.Ticks,
                      v => ParseTimeSpanFromDb(v));
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasIndex(e => e.Date);
            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.Payments)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasIndex(e => e.Date);
            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.MedicalRecords)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ToothRecord>(entity =>
        {
            entity.HasIndex(e => new { e.PatientId, e.ToothNumber }).IsUnique();
            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.ToothRecords)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<XRayRecord>(entity =>
        {
            entity.HasIndex(e => e.PatientId);
            entity.HasOne(e => e.Patient)
                  .WithMany(p => p.XRayRecords)
                  .HasForeignKey(e => e.PatientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
        });

        modelBuilder.Entity<StaffMember>(entity =>
        {
            entity.HasIndex(e => e.FullName);
            entity.HasIndex(e => e.Role);
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Category);
        });
    }

    private static TimeSpan ParseTimeSpanFromDb(object? v)
    {
        if (v == null || v == DBNull.Value) return new TimeSpan(9, 0, 0);
        if (v is long ticks) return TimeSpan.FromTicks(ticks);
        var s = v.ToString();
        if (string.IsNullOrWhiteSpace(s)) return new TimeSpan(9, 0, 0);
        if (long.TryParse(s, System.Globalization.NumberStyles.Integer, null, out var t))
            return TimeSpan.FromTicks(t);
        if (TimeSpan.TryParse(s, out var ts)) return ts;
        var parts = s.Split(':');
        if (parts.Length >= 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            return new TimeSpan(h, m, 0);
        return new TimeSpan(9, 0, 0);
    }

    public static string GetDatabasePath() => DbPath;

    public static string GetDatabaseDirectory() => DbDirectory;

    public void EnsureCreated()
    {
        Database.EnsureCreated();
        MigrateSchema();
    }

    private void MigrateSchema()
    {
        using var conn = new SqliteConnection(Database.GetConnectionString());
        conn.Open();

        AddColumnIfMissing(conn, "Patients", "PhotoPath", "TEXT DEFAULT '' NOT NULL");
        CreateTableIfMissing(conn);
        CreateXRayTableIfMissing(conn);
        CreateUsersTableIfMissing(conn);
        CreateAuditLogTableIfMissing(conn);
        MigratePatientProFields(conn);
        MigratePaymentMoroccoFields(conn);
        AddColumnIfMissing(conn, "Users", "MustChangePassword", "INTEGER DEFAULT 0 NOT NULL");
        CreateStaffTableIfMissing(conn);
        CreateInventoryTableIfMissing(conn);
    }

    private static void CreateStaffTableIfMissing(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS StaffMembers (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FullName TEXT NOT NULL,
                Role INTEGER NOT NULL DEFAULT 3,
                Phone TEXT DEFAULT '' NOT NULL,
                Email TEXT DEFAULT '' NOT NULL,
                Specialization TEXT DEFAULT '' NOT NULL,
                HireDate TEXT NOT NULL DEFAULT (date('now')),
                Status INTEGER NOT NULL DEFAULT 0,
                Notes TEXT DEFAULT '' NOT NULL,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            )";
        cmd.ExecuteNonQuery();
    }

    private static void CreateInventoryTableIfMissing(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS InventoryItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Category TEXT DEFAULT '' NOT NULL,
                Quantity INTEGER NOT NULL DEFAULT 0,
                Unit TEXT DEFAULT 'pcs' NOT NULL,
                MinStockLevel INTEGER NOT NULL DEFAULT 0,
                UnitPrice REAL NOT NULL DEFAULT 0,
                Supplier TEXT DEFAULT '' NOT NULL,
                LastRestockedDate TEXT,
                Notes TEXT DEFAULT '' NOT NULL,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            )";
        cmd.ExecuteNonQuery();
    }

    private static void CreateUsersTableIfMissing(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL,
                Role INTEGER NOT NULL DEFAULT 3,
                Status INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now')),
                LastLogin TEXT,
                FailedLoginAttempts INTEGER NOT NULL DEFAULT 0,
                LockoutEnd TEXT
            )";
        cmd.ExecuteNonQuery();
        using var idx = conn.CreateCommand();
        idx.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Username ON Users (Username)";
        idx.ExecuteNonQuery();
    }

    private static void CreateAuditLogTableIfMissing(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS AuditLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Action TEXT NOT NULL,
                Timestamp TEXT NOT NULL DEFAULT (datetime('now')),
                Details TEXT
            )";
        cmd.ExecuteNonQuery();
        using var idx = conn.CreateCommand();
        idx.CommandText = "CREATE INDEX IF NOT EXISTS IX_AuditLogs_UserId ON AuditLogs (UserId)";
        idx.ExecuteNonQuery();
        using var idx2 = conn.CreateCommand();
        idx2.CommandText = "CREATE INDEX IF NOT EXISTS IX_AuditLogs_Timestamp ON AuditLogs (Timestamp)";
        idx2.ExecuteNonQuery();
    }

    private static void MigratePatientProFields(SqliteConnection conn)
    {
        AddColumnIfMissing(conn, "Patients", "CIN", "TEXT DEFAULT '' NOT NULL");
        AddColumnIfMissing(conn, "Patients", "EmergencyContact", "TEXT DEFAULT '' NOT NULL");
        AddColumnIfMissing(conn, "Patients", "RegistrationDate", "TEXT DEFAULT (date('now')) NOT NULL");
        AddColumnIfMissing(conn, "Patients", "Allergies", "TEXT DEFAULT '' NOT NULL");
        AddColumnIfMissing(conn, "Patients", "Medications", "TEXT DEFAULT '' NOT NULL");
        AddColumnIfMissing(conn, "Patients", "ChronicDiseases", "TEXT DEFAULT '' NOT NULL");
        AddColumnIfMissing(conn, "Patients", "PregnancyStatus", "TEXT DEFAULT '' NOT NULL");
        AddColumnIfMissing(conn, "Patients", "CNSSNumber", "TEXT DEFAULT '' NOT NULL");
        AddColumnIfMissing(conn, "Patients", "CNSSCoverageType", "TEXT DEFAULT '' NOT NULL");
        AddColumnIfMissing(conn, "Patients", "CNSSRegistrationDate", "TEXT");
        AddColumnIfMissing(conn, "Patients", "CNSSValidityDate", "TEXT");
    }

    private static void MigratePaymentMoroccoFields(SqliteConnection conn)
    {
        AddColumnIfMissing(conn, "Payments", "TreatmentCost", "REAL DEFAULT 0 NOT NULL");
        AddColumnIfMissing(conn, "Payments", "CNSSCoveredAmount", "REAL DEFAULT 0 NOT NULL");
        AddColumnIfMissing(conn, "Payments", "PatientAmount", "REAL DEFAULT 0 NOT NULL");
        AddColumnIfMissing(conn, "Payments", "DiscountAmount", "REAL DEFAULT 0 NOT NULL");
        AddColumnIfMissing(conn, "Payments", "VATRate", "REAL DEFAULT 20 NOT NULL");
        AddColumnIfMissing(conn, "Payments", "VATAmount", "REAL DEFAULT 0 NOT NULL");
        AddColumnIfMissing(conn, "Payments", "Currency", "TEXT DEFAULT 'MAD' NOT NULL");
        AddColumnIfMissing(conn, "Payments", "CNSSClaimStatus", "INTEGER DEFAULT 0 NOT NULL");
        AddColumnIfMissing(conn, "Payments", "CNSSReceiptNumber", "TEXT DEFAULT '' NOT NULL");
    }

    private static void AddColumnIfMissing(SqliteConnection conn, string table, string column, string definition)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            if (reader.GetString(1).Equals(column, StringComparison.OrdinalIgnoreCase))
                return;
        }
        reader.Close();

        using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
        alter.ExecuteNonQuery();
    }

    private static void CreateTableIfMissing(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ToothRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PatientId INTEGER NOT NULL,
                ToothNumber INTEGER NOT NULL,
                Type INTEGER NOT NULL DEFAULT 0,
                Condition INTEGER NOT NULL DEFAULT 0,
                Notes TEXT NOT NULL DEFAULT '',
                LastUpdated TEXT NOT NULL DEFAULT '2026-01-01',
                FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE CASCADE
            )";
        cmd.ExecuteNonQuery();

        using var idx = conn.CreateCommand();
        idx.CommandText = @"
            CREATE UNIQUE INDEX IF NOT EXISTS IX_ToothRecords_PatientId_ToothNumber 
            ON ToothRecords (PatientId, ToothNumber)";
        idx.ExecuteNonQuery();
    }

    private static void CreateXRayTableIfMissing(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS XRayRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PatientId INTEGER NOT NULL,
                ImagePath TEXT NOT NULL DEFAULT '',
                Date TEXT NOT NULL DEFAULT '2026-01-01',
                Notes TEXT NOT NULL DEFAULT '',
                ToothNumbers TEXT NOT NULL DEFAULT '',
                FOREIGN KEY (PatientId) REFERENCES Patients(Id) ON DELETE CASCADE
            )";
        cmd.ExecuteNonQuery();

        using var idx = conn.CreateCommand();
        idx.CommandText = "CREATE INDEX IF NOT EXISTS IX_XRayRecords_PatientId ON XRayRecords (PatientId)";
        idx.ExecuteNonQuery();
    }
}
