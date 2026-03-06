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
    }

    public static string GetDatabasePath() => DbPath;

    public static string GetDatabaseDirectory() => DbDirectory;

    public void EnsureCreated()
    {
        Database.EnsureCreated();
    }
}
