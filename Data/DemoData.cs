using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ProjetDotNet.Data;

/// <summary>Creates fictitious data only in the fresh SQLite database selected by --demo.</summary>
public static class DemoData
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        if (!db.Database.IsSqlite()) throw new InvalidOperationException("Demo requires isolated SQLite.");
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedDefaultData(services);
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        foreach (var role in new[] { "Admin", "User" })
        {
            var password = configuration[$"Demo:{role}Password"]
                ?? throw new InvalidOperationException($"Set Demo__{role}Password locally before using --demo.");
            var email = role == "Admin" ? "admin@lustre.example" : "customer@lustre.example";
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException("Demo account creation failed: check password policy.");
            result = await users.AddToRoleAsync(user, role);
            if (!result.Succeeded) throw new InvalidOperationException("Demo role assignment failed.");
        }
        var category = new Category { CategoryName = "Daily skincare" };
        var images = new[] { "d619970b-21eb-4414-88be-7e71820ffb72.png", "f3e17ab7-5837-4354-94a3-73d03fd46e1b.png", "fb79f4d7-4bb6-40f0-93e0-10b8344b6c15.png", "505c394b-70d2-409f-86b7-49bfaf537940.png" };
        var names = new[] { "Gentle Cleanser", "Hydrating Moisturizer", "Niacinamide Serum", "SPF 60 Sunscreen" };
        for (int i = 0; i < names.Length; i++)
        {
            db.Products.Add(new Product
            {
                ProductName = names[i],
                BrandName = "Lustre",
                Category = category,
                Price = 120m + i * 25m,
                Image = images.Length > 0 ? Path.GetFileName(images[i % images.Length]) : null,
                Stock = new Stock { Quantity = i == 3 ? 0 : 20 }
            });
        }
        await db.SaveChangesAsync();
    }
}
