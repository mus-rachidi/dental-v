using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Database;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class ToothService
{
    public async Task<List<ToothRecord>> GetByPatientAsync(int patientId)
    {
        using var db = new ClinicDbContext();
        return await db.ToothRecords
            .Where(t => t.PatientId == patientId)
            .OrderBy(t => t.ToothNumber)
            .ToListAsync();
    }

    public async Task SaveAsync(ToothRecord record)
    {
        record.LastUpdated = DateTime.Now;

        using var db = new ClinicDbContext();
        var existing = await db.ToothRecords
            .FirstOrDefaultAsync(t => t.PatientId == record.PatientId && t.ToothNumber == record.ToothNumber);

        if (existing != null)
        {
            existing.Condition = record.Condition;
            existing.Type = record.Type;
            existing.Notes = record.Notes;
            existing.LastUpdated = record.LastUpdated;
        }
        else
        {
            db.ToothRecords.Add(record);
        }

        await db.SaveChangesAsync();
    }

    public async Task InitializePatientTeethAsync(int patientId)
    {
        using var db = new ClinicDbContext();
        var existing = await db.ToothRecords.AnyAsync(t => t.PatientId == patientId);
        if (existing) return;

        var teeth = new List<ToothRecord>();
        for (int i = 1; i <= 32; i++)
        {
            teeth.Add(new ToothRecord
            {
                PatientId = patientId,
                ToothNumber = i,
                Type = GetToothType(i),
                Condition = ToothCondition.Healthy
            });
        }

        db.ToothRecords.AddRange(teeth);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Standard dental numbering (Universal/ADA):
    /// Upper right: 1-8, Upper left: 9-16
    /// Lower left: 17-24, Lower right: 25-32
    /// </summary>
    public static ToothType GetToothType(int number)
    {
        int pos = number switch
        {
            >= 1 and <= 8 => number,
            >= 9 and <= 16 => 17 - number,
            >= 17 and <= 24 => number - 16,
            >= 25 and <= 32 => 33 - number,
            _ => 1
        };

        return pos switch
        {
            1 or 2 => ToothType.Incisor,
            3 => ToothType.Canine,
            4 or 5 => ToothType.Premolar,
            6 or 7 => ToothType.Molar,
            8 => ToothType.WisdomTooth,
            _ => ToothType.Molar
        };
    }

    public static string GetToothName(int number)
    {
        var type = GetToothType(number);
        string quadrant = number switch
        {
            >= 1 and <= 8 => "Upper Right",
            >= 9 and <= 16 => "Upper Left",
            >= 17 and <= 24 => "Lower Left",
            >= 25 and <= 32 => "Lower Right",
            _ => ""
        };
        return $"#{number} - {quadrant} {type}";
    }

    public static string SavePatientPhoto(int patientId, string sourcePath)
    {
        var photosDir = Path.Combine(ClinicDbContext.GetDatabaseDirectory(), "Photos");
        Directory.CreateDirectory(photosDir);

        var ext = Path.GetExtension(sourcePath);
        var fileName = $"patient_{patientId}{ext}";
        var destPath = Path.Combine(photosDir, fileName);

        File.Copy(sourcePath, destPath, overwrite: true);
        return destPath;
    }
}
