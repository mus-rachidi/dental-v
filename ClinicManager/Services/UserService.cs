using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClinicManager.Database;
using ClinicManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class UserService
{
    public async Task<List<User>> GetAllAsync()
    {
        await using var db = new ClinicDbContext();
        return await db.Users.OrderBy(u => u.Username).ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        await using var db = new ClinicDbContext();
        return await db.Users.FindAsync(id);
    }
}
