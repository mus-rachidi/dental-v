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
