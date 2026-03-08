using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Database;
using ClinicManager.Models;

namespace ClinicManager.Services;

public class InventoryService
{
    public async Task<List<InventoryItem>> GetAllAsync()
    {
        using var db = new ClinicDbContext();
        return await db.InventoryItems
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<List<InventoryItem>> GetByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return await GetAllAsync();

        using var db = new ClinicDbContext();
        return await db.InventoryItems
            .Where(i => i.Category == category)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        using var db = new ClinicDbContext();
        return await db.InventoryItems
            .Where(i => !string.IsNullOrEmpty(i.Category))
            .Select(i => i.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();
    }

    public async Task<InventoryItem?> GetByIdAsync(int id)
    {
        using var db = new ClinicDbContext();
        return await db.InventoryItems.FindAsync(id);
    }

    public async Task<List<InventoryItem>> GetLowStockAsync()
    {
        using var db = new ClinicDbContext();
        return await db.InventoryItems
            .Where(i => i.MinStockLevel > 0 && i.Quantity <= i.MinStockLevel)
            .OrderBy(i => i.Quantity)
            .ToListAsync();
    }

    public async Task<List<InventoryItem>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        using var db = new ClinicDbContext();
        var lower = query.ToLower();
        return await db.InventoryItems
            .Where(i => (i.Name != null && i.Name.ToLower().Contains(lower))
                     || (i.Category != null && i.Category.ToLower().Contains(lower))
                     || (i.Supplier != null && i.Supplier.ToLower().Contains(lower)))
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<InventoryItem> CreateAsync(InventoryItem item)
    {
        item.CreatedAt = DateTime.Now;
        using var db = new ClinicDbContext();
        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    public async Task UpdateAsync(InventoryItem item)
    {
        using var db = new ClinicDbContext();
        db.InventoryItems.Update(item);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new ClinicDbContext();
        var item = await db.InventoryItems.FindAsync(id);
        if (item != null)
        {
            db.InventoryItems.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    public async Task RestockAsync(int id, int quantity)
    {
        var item = await GetByIdAsync(id);
        if (item == null) return;

        item.Quantity += quantity;
        item.LastRestockedDate = DateTime.Now;
        await UpdateAsync(item);
    }
}
