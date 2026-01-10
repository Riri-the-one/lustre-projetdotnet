using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjetDotNet.Constants;
using ProjetDotNet.Data;
using ProjetDotNet.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjetDotNet.Data
{
    public class DbSeeder
    {
        public static async Task SeedDefaultData(IServiceProvider service)
        {
            try
            {
                var context = service.GetRequiredService<ApplicationDbContext>();

                // Apply migrations
                if ((await context.Database.GetPendingMigrationsAsync()).Any())
                {
                    await context.Database.MigrateAsync();
                }

                var userMgr = service.GetRequiredService<UserManager<IdentityUser>>();
                var roleMgr = service.GetRequiredService<RoleManager<IdentityRole>>();

                // -------- Roles --------
                if (!await roleMgr.RoleExistsAsync(Roles.Admin.ToString()))
                    await roleMgr.CreateAsync(new IdentityRole(Roles.Admin.ToString()));

                if (!await roleMgr.RoleExistsAsync(Roles.User.ToString()))
                    await roleMgr.CreateAsync(new IdentityRole(Roles.User.ToString()));

                // -------- Admin User --------
                var adminEmail = "admin@gmail.com";
                var admin = await userMgr.FindByEmailAsync(adminEmail);

                if (admin == null)
                {
                    admin = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    await userMgr.CreateAsync(admin, "Admin@123");
                    await userMgr.AddToRoleAsync(admin, Roles.Admin.ToString());
                }

                // -------- Categories --------
                if (!context.Categories.Any())
                {
                    var categories = new List<Category>
                    {
                        new Category { CategoryName = "Cleanser" },
                        new Category { CategoryName = "Moisturizer" },
                        new Category { CategoryName = "Serum" },
                        new Category { CategoryName = "Sunscreen" },
                        new Category { CategoryName = "Mask" }
                    };

                    await context.Categories.AddRangeAsync(categories);
                    await context.SaveChangesAsync();
                }

                // -------- Products --------
                if (!context.Products.Any())
                {
                    var products = new List<Product>
                    {
                        new Product { ProductName = "Gentle Cleanser", Price = 120, CategoryId = 1 },
                        new Product { ProductName = "Hydrating Moisturizer", Price = 180, CategoryId = 2 },
                        new Product {ProductName = "Vitamin C Serum", Price = 250, CategoryId = 3 },
                        new Product { ProductName = "SPF 50 Sunscreen", Price = 200, CategoryId = 4 },
                        new Product { ProductName = "Clay Face Mask", Price = 150, CategoryId = 5 }
                    };

                    await context.Products.AddRangeAsync(products);
                    await context.SaveChangesAsync();
                }

                // -------- Order Status --------
                var existingStatuses = await context.OrderStatuses.ToListAsync();
                var existingStatusIds = existingStatuses.Select(s => s.StatusId).ToHashSet();
                var existingStatusNames = existingStatuses.Select(s => s.StatusName?.ToLowerInvariant()).Where(n => n != null).ToHashSet();
                
                var requiredStatuses = new Dictionary<string, int>
                {
                    { "Pending", 1 },
                    { "Approved", 2 },
                    { "Shipped", 3 },
                    { "Delivered", 4 },
                    { "Cancelled", 5 }
                };

                foreach (var status in requiredStatuses)
                {
                    // Vérifier si le nom du statut existe déjà (peu importe le StatusId)
                    bool statusNameExists = existingStatusNames.Contains(status.Key.ToLowerInvariant());
                    
                    if (!statusNameExists)
                    {
                        // Si le StatusId est déjà utilisé, trouver le prochain disponible
                        int statusIdToUse = status.Value;
                        if (existingStatusIds.Contains(statusIdToUse))
                        {
                            // Trouver le prochain StatusId disponible
                            int maxStatusId = existingStatusIds.Any() ? existingStatusIds.Max() : 0;
                            statusIdToUse = maxStatusId + 1;
                        }
                        
                        var orderStatus = new OrderStatus 
                        { 
                            StatusId = statusIdToUse, 
                            StatusName = status.Key 
                        };
                        context.OrderStatuses.Add(orderStatus);
                        existingStatusIds.Add(statusIdToUse);
                        existingStatusNames.Add(status.Key.ToLowerInvariant());
                    }
                }

                if (context.ChangeTracker.HasChanges())
                {
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
