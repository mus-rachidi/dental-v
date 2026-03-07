using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Database;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class AppointmentService
{
    public async Task<List<Appointment>> GetAllAsync()
    {
        using var db = new ClinicDbContext();
        var list = await db.Appointments.Include(a => a.Patient).ToListAsync();
        return list.OrderByDescending(a => a.Date).ThenBy(a => a.Time).ToList();
    }

    public async Task<List<Appointment>> GetTodayAsync()
    {
        var today = DateTime.Today;
        using var db = new ClinicDbContext();
        var list = await db.Appointments
            .Include(a => a.Patient)
            .Where(a => a.Date.Date == today)
            .ToListAsync();
        return list.OrderBy(a => a.Time).ToList();
    }

    public async Task<List<Appointment>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var db = new ClinicDbContext();
        var list = await db.Appointments
            .Include(a => a.Patient)
            .Where(a => a.Date.Date >= from.Date && a.Date.Date <= to.Date)
            .ToListAsync();
        return list.OrderBy(a => a.Date).ThenBy(a => a.Time).ToList();
    }

    public async Task<List<Appointment>> GetByPatientAsync(int patientId)
    {
        using var db = new ClinicDbContext();
        return await db.Appointments
            .Include(a => a.Patient)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<Appointment> CreateAsync(Appointment appointment)
    {
        appointment.CreatedAt = DateTime.Now;

        using var db = new ClinicDbContext();
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        return appointment;
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        using var db = new ClinicDbContext();
        db.Appointments.Update(appointment);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new ClinicDbContext();
        var appt = await db.Appointments.FindAsync(id);
        if (appt != null)
        {
            db.Appointments.Remove(appt);
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateStatusAsync(int id, AppointmentStatus status)
    {
        using var db = new ClinicDbContext();
        var appt = await db.Appointments.FindAsync(id);
        if (appt != null)
        {
            appt.Status = status;
            await db.SaveChangesAsync();
        }
    }

    public async Task<int> GetTodayCountAsync()
    {
        var today = DateTime.Today;
        using var db = new ClinicDbContext();
        return await db.Appointments.CountAsync(a => a.Date.Date == today);
    }

    public async Task<int> GetUpcomingCountAsync()
    {
        var now = DateTime.Now;
        using var db = new ClinicDbContext();
        return await db.Appointments
            .CountAsync(a => a.Date.Date >= now.Date && a.Status == AppointmentStatus.Scheduled);
    }
}
