using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Database;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class PatientService
{
    public async Task<List<Patient>> GetAllAsync()
    {
        using var db = new ClinicDbContext();
        return await db.Patients.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<Patient?> GetByIdAsync(int id)
    {
        using var db = new ClinicDbContext();
        return await db.Patients
            .Include(p => p.Appointments)
            .Include(p => p.Payments)
            .Include(p => p.MedicalRecords)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Patient>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        using var db = new ClinicDbContext();
        var lower = query.ToLower();
        return await db.Patients
            .Where(p => (p.FullName != null && p.FullName.ToLower().Contains(lower))
                     || (p.Phone != null && p.Phone.Contains(query))
                     || (p.Email != null && p.Email.ToLower().Contains(lower)))
            .OrderBy(p => p.FullName)
            .ToListAsync();
    }

    public async Task<Patient> CreateAsync(Patient patient)
    {
        patient.CreatedAt = DateTime.Now;
        patient.UpdatedAt = DateTime.Now;

        using var db = new ClinicDbContext();
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return patient;
    }

    public async Task UpdateAsync(Patient patient)
    {
        patient.UpdatedAt = DateTime.Now;

        using var db = new ClinicDbContext();
        db.Patients.Update(patient);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new ClinicDbContext();
        var patient = await db.Patients.FindAsync(id);
        if (patient != null)
        {
            db.Patients.Remove(patient);
            await db.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        using var db = new ClinicDbContext();
        return await db.Patients.CountAsync();
    }

    public async Task<List<Patient>> GetRecentAsync(int count = 5)
    {
        using var db = new ClinicDbContext();
        return await db.Patients
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetNewPatientsThisMonthAsync()
    {
        var firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        using var db = new ClinicDbContext();
        return await db.Patients.CountAsync(p => p.CreatedAt >= firstDay);
    }

    public async Task<(int Male, int Female, int Other)> GetGenderDemographicsAsync()
    {
        using var db = new ClinicDbContext();
        var patients = await db.Patients.ToListAsync();
        var male = patients.Count(p => string.Equals(p.Gender, "Male", StringComparison.OrdinalIgnoreCase));
        var female = patients.Count(p => string.Equals(p.Gender, "Female", StringComparison.OrdinalIgnoreCase));
        var other = patients.Count - male - female;
        return (male, female, Math.Max(0, other));
    }
}
