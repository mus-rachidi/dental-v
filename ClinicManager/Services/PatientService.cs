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
            .Where(p => p.FullName.ToLower().Contains(lower)
                     || p.Phone.Contains(query)
                     || p.Email.ToLower().Contains(lower))
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
}
