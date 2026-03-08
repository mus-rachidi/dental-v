using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Database;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class XRayService
{
    public async Task<List<XRayRecord>> GetByPatientAsync(int patientId)
    {
        using var db = new ClinicDbContext();
        return await db.XRayRecords
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.Date)
            .ToListAsync();
    }

    public async Task SaveAsync(XRayRecord record)
    {
        using var db = new ClinicDbContext();
        if (record.Id == 0)
            db.XRayRecords.Add(record);
        else
            db.XRayRecords.Update(record);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new ClinicDbContext();
        var r = await db.XRayRecords.FindAsync(id);
        if (r != null)
        {
            if (!string.IsNullOrEmpty(r.ImagePath) && File.Exists(r.ImagePath))
                try { File.Delete(r.ImagePath); } catch { }
            db.XRayRecords.Remove(r);
            await db.SaveChangesAsync();
        }
    }

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
    private const int MaxFileSizeBytes = 10 * 1024 * 1024;

    public static string SaveXRayImage(int patientId, string sourcePath)
    {
        ValidateImagePath(sourcePath);

        var dir = Path.Combine(ClinicDbContext.GetDatabaseDirectory(), "XRays");
        Directory.CreateDirectory(dir);

        var ext = Path.GetExtension(sourcePath);
        var fileName = $"xray_{patientId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
        var destPath = Path.Combine(dir, fileName);

        File.Copy(sourcePath, destPath, overwrite: true);
        return destPath;
    }

    private static void ValidateImagePath(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("File path is required.");

        var fullPath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("File not found.", fullPath);

        var ext = Path.GetExtension(fullPath);
        if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext.ToLowerInvariant()))
            throw new ArgumentException($"Invalid file type. Allowed: {string.Join(", ", AllowedImageExtensions)}");

        var info = new FileInfo(fullPath);
        if (info.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"File too large. Maximum size: {MaxFileSizeBytes / (1024 * 1024)} MB.");
    }
}
