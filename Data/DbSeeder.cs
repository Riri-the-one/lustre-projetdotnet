using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ProjetDotNet.Data;

/// <summary>Explicit role provisioning only; never called during normal startup.</summary>
public static class DbSeeder
{
    public static async Task SeedDefaultData(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var name in new[] { "Admin", "User" })
        {
            if (await roles.RoleExistsAsync(name)) continue;
            var result = await roles.CreateAsync(new IdentityRole(name));
            if (!result.Succeeded)
                throw new InvalidOperationException("Role provisioning failed.");
        }
        var db = services.GetRequiredService<ApplicationDbContext>();
        var existing = await db.OrderStatuses.ToListAsync();
        var nextCode = existing.Select(s => s.StatusId).DefaultIfEmpty().Max();
        foreach (var name in new[] { "Pending", "Approved", "Shipped", "Delivered", "Cancelled" })
        {
            if (existing.Any(s => string.Equals(s.StatusName, name, StringComparison.OrdinalIgnoreCase))) continue;
            db.OrderStatuses.Add(new ProjetDotNet.Models.OrderStatus { StatusName = name, StatusId = ++nextCode });
        }
        await db.SaveChangesAsync();
    }
}
