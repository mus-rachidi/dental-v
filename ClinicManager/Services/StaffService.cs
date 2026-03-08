using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Database;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class StaffService
{
    public async Task<List<StaffMember>> GetAllAsync()
    {
        using var db = new ClinicDbContext();
        return await db.StaffMembers
            .OrderBy(s => s.FullName)
            .ToListAsync();
    }

    public async Task<List<StaffMember>> GetActiveAsync()
    {
        using var db = new ClinicDbContext();
        return await db.StaffMembers
            .Where(s => s.Status == StaffStatus.Active)
            .OrderBy(s => s.FullName)
            .ToListAsync();
    }

    public async Task<StaffMember?> GetByIdAsync(int id)
    {
        using var db = new ClinicDbContext();
        return await db.StaffMembers.FindAsync(id);
    }

    public async Task<List<StaffMember>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        using var db = new ClinicDbContext();
        var lower = query.ToLower();
        return await db.StaffMembers
            .Where(s => (s.FullName != null && s.FullName.ToLower().Contains(lower))
                     || (s.Email != null && s.Email.ToLower().Contains(lower))
                     || (s.Phone != null && s.Phone.Contains(query))
                     || (s.Specialization != null && s.Specialization.ToLower().Contains(lower)))
            .OrderBy(s => s.FullName)
            .ToListAsync();
    }

    public async Task<StaffMember> CreateAsync(StaffMember staff)
    {
        staff.CreatedAt = DateTime.Now;
        using var db = new ClinicDbContext();
        db.StaffMembers.Add(staff);
        await db.SaveChangesAsync();
        return staff;
    }

    public async Task UpdateAsync(StaffMember staff)
    {
        using var db = new ClinicDbContext();
        db.StaffMembers.Update(staff);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new ClinicDbContext();
        var staff = await db.StaffMembers.FindAsync(id);
        if (staff != null)
        {
            db.StaffMembers.Remove(staff);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetDoctorNamesAsync()
    {
        var staff = await GetActiveAsync();
        return staff
            .Where(s => s.Role == StaffRole.Dentist || s.Role == StaffRole.Hygienist)
            .Select(s => s.FullName)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }
}
