using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Seeders;

public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        IConfiguration config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(AdminSeeder).FullName!);

        string? email = config["Admin:Email"];
        string? fullName = config["Admin:FullName"];
        string? password = config["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Admin seeding skipped: Admin section is not configured.");
            return;
        }

        UserManager<AppUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        AppUser? existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Make sure existing user has Admin role
            if (!await userManager.IsInRoleAsync(existing, UserRole.Admin.ToString()))
                await userManager.AddToRoleAsync(existing, UserRole.Admin.ToString());
            return;
        }

        AppUser admin = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        IdentityResult create = await userManager.CreateAsync(admin, password);
        if (!create.Succeeded)
        {
            logger.LogError("Failed to create admin user: {Errors}",
                string.Join("; ", create.Errors.Select(e => e.Description)));
            return;
        }

        IdentityResult role = await userManager.AddToRoleAsync(admin, UserRole.Admin.ToString());
        if (!role.Succeeded)
        {
            logger.LogError("Failed to assign Admin role: {Errors}",
                string.Join("; ", role.Errors.Select(e => e.Description)));
            await userManager.DeleteAsync(admin);
            return;
        }

        logger.LogInformation("Admin user '{Email}' created and assigned Admin role.", email);
    }
}
