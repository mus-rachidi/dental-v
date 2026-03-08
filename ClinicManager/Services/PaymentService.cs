using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Database;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class PaymentService
{
    public async Task<List<Payment>> GetAllAsync()
    {
        using var db = new ClinicDbContext();
        return await db.Payments
            .Include(p => p.Patient)
            .OrderByDescending(p => p.Date)
            .ToListAsync();
    }

    public async Task<List<Payment>> GetByPatientAsync(int patientId)
    {
        using var db = new ClinicDbContext();
        return await db.Payments
            .Include(p => p.Patient)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.Date)
            .ToListAsync();
    }

    public async Task<List<Payment>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        using var db = new ClinicDbContext();
        return await db.Payments
            .Include(p => p.Patient)
            .Where(p => p.Date.Date >= from.Date && p.Date.Date <= to.Date)
            .OrderByDescending(p => p.Date)
            .ToListAsync();
    }

    public async Task<Payment> CreateAsync(Payment payment)
    {
        payment.CreatedAt = DateTime.Now;

        if (string.IsNullOrEmpty(payment.InvoiceNumber))
            payment.InvoiceNumber = await GenerateInvoiceNumberAsync();

        // Morocco: ensure CNSS fields have defaults; backward compat: TreatmentCost = Amount if not set
        if (payment.TreatmentCost == 0) payment.TreatmentCost = payment.Amount;
        if (payment.PatientAmount == 0) payment.PatientAmount = payment.Amount - payment.CNSSCoveredAmount - payment.DiscountAmount;
        if (payment.PatientAmount < 0) payment.PatientAmount = 0;
        if (payment.VATAmount == 0 && payment.VATRate > 0)
            payment.VATAmount = Helpers.MoroccoFormatting.CalculateVAT(payment.TreatmentCost, payment.VATRate);
        if (string.IsNullOrEmpty(payment.Currency)) payment.Currency = "MAD";

        using var db = new ClinicDbContext();
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return payment;
    }

    public async Task UpdateAsync(Payment payment)
    {
        using var db = new ClinicDbContext();
        db.Payments.Update(payment);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new ClinicDbContext();
        var payment = await db.Payments.FindAsync(id);
        if (payment != null)
        {
            db.Payments.Remove(payment);
            await db.SaveChangesAsync();
        }
    }

    public async Task<decimal> GetTodayRevenueAsync()
    {
        var today = DateTime.Today;
        using var db = new ClinicDbContext();
        return await db.Payments
            .Where(p => p.Date.Date == today && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount);
    }

    public async Task<decimal> GetMonthRevenueAsync()
    {
        var firstDay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        using var db = new ClinicDbContext();
        return await db.Payments
            .Where(p => p.Date >= firstDay && p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount);
    }

    public async Task<decimal> GetTotalRevenueAsync()
    {
        using var db = new ClinicDbContext();
        return await db.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .SumAsync(p => p.Amount);
    }

    /// <summary>Get payments for CNSS report (Morocco).</summary>
    public async Task<List<Payment>> GetCNSSReportAsync(DateTime from, DateTime to)
    {
        using var db = new ClinicDbContext();
        return await db.Payments
            .Include(p => p.Patient)
            .Where(p => p.Date.Date >= from.Date && p.Date.Date <= to.Date
                && p.CNSSCoveredAmount > 0)
            .OrderByDescending(p => p.Date)
            .ToListAsync();
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        using var db = new ClinicDbContext();
        var count = await db.Payments.CountAsync();
        return $"INV-{DateTime.Now:yyyyMM}-{count + 1:D4}";
    }
}
