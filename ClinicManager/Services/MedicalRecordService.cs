using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Database;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class MedicalRecordService
{
    public async Task<List<MedicalRecord>> GetAllAsync()
    {
        using var db = new ClinicDbContext();
        return await db.MedicalRecords
            .Include(r => r.Patient)
            .OrderByDescending(r => r.Date)
            .ToListAsync();
    }

    public async Task<List<MedicalRecord>> GetByPatientAsync(int patientId)
    {
        using var db = new ClinicDbContext();
        return await db.MedicalRecords
            .Include(r => r.Patient)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.Date)
            .ToListAsync();
    }

    public async Task<MedicalRecord?> GetByIdAsync(int id)
    {
        using var db = new ClinicDbContext();
        return await db.MedicalRecords
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<MedicalRecord> CreateAsync(MedicalRecord record)
    {
        record.CreatedAt = DateTime.Now;

        using var db = new ClinicDbContext();
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync();
        return record;
    }

    public async Task UpdateAsync(MedicalRecord record)
    {
        using var db = new ClinicDbContext();
        db.MedicalRecords.Update(record);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new ClinicDbContext();
        var record = await db.MedicalRecords.FindAsync(id);
        if (record != null)
        {
            db.MedicalRecords.Remove(record);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<MedicalRecord>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        using var db = new ClinicDbContext();
        var lower = query.ToLower();
        return await db.MedicalRecords
            .Include(r => r.Patient)
            .Where(r => r.Diagnosis.ToLower().Contains(lower)
                     || r.Prescription.ToLower().Contains(lower)
                     || r.Notes.ToLower().Contains(lower)
                     || (r.Patient != null && r.Patient.FullName.ToLower().Contains(lower)))
            .OrderByDescending(r => r.Date)
            .ToListAsync();
    }
}
