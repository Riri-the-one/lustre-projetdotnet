using Microsoft.AspNetCore.Identity;

namespace ProjetDotNet.Data;

public static class AdminProvisioner
{
    public static async Task CreateAsync(IServiceProvider services, IConfiguration config)
    {
        var email = config["BootstrapAdmin:Email"];
        var password = config["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Set BootstrapAdmin__Email and BootstrapAdmin__Password using local secrets.");
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        if (await users.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("Account already exists. Existing users are never modified by provisioning.");
        await DbSeeder.SeedDefaultData(services);
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) throw new InvalidOperationException("Administrator creation failed. Check the password policy and email.");
        result = await users.AddToRoleAsync(user, "Admin");
        if (!result.Succeeded)
        {
            var deleted = await users.DeleteAsync(user);
            if (!deleted.Succeeded) throw new InvalidOperationException("Role assignment failed; manual cleanup of the new account is required.");
            throw new InvalidOperationException("Administrator role assignment failed; the new account was removed.");
        }
    }
}
