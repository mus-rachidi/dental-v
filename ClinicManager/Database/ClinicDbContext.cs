using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Models;

namespace ClinicManager.Database;

public class ClinicDbContext : DbContext
{
    private static readonly string DbDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClinicManager");

    private static readonly string DbPath = Path.Combine(DbDirectory, "clinic.db");

    // Password for SQLCipher-style encryption via SQLite connection string
    private const string DbPassword = "Cl1n!cM@nager#2026$Secure";

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        Directory.CreateDirectory(DbDirectory);

        var connectionString = $"Data Source={DbPath};Password={DbPassword}";
        optionsBuilder.UseSqlite(connectionString);

#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
#endif
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
    }

    public static string GetDatabasePath() => DbPath;

    public static string GetDatabaseDirectory() => DbDirectory;

    public void EnsureCreated()
    {
        Database.EnsureCreated();
    }
}
