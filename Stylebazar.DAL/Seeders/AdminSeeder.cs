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
            bool wasAdmin = await userManager.IsInRoleAsync(existing, UserRole.Admin.ToString());

            // Promote to Admin if not already in the role.
            if (!wasAdmin)
                await userManager.AddToRoleAsync(existing, UserRole.Admin.ToString());

            // ONE-TIME password reset on promotion only. Without this, taking
            // an existing user (e.g. a previously-registered Buyer) and making
            // them admin via Admin__Email config leaves them with their OLD
            // password — the config password never gets applied. Once they're
            // already in the Admin role, subsequent restarts skip this so a
            // self-service password change via /Account/Profile isn't reverted
            // every time the app boots.
            if (!wasAdmin)
            {
                string resetToken = await userManager.GeneratePasswordResetTokenAsync(existing);
                await userManager.ResetPasswordAsync(existing, resetToken, password);

                // Make sure FullName reflects the configured admin name and
                // the email is treated as confirmed (Identity uses
                // EmailConfirmed for token-based flows like password reset).
                existing.FullName = fullName;
                existing.EmailConfirmed = true;
                if (existing.CreatedDate == default) existing.CreatedDate = DateTime.UtcNow;
                await userManager.UpdateAsync(existing);

                logger.LogInformation(
                    "Existing user '{Email}' promoted to Admin and password synced to Admin:Password config.",
                    email);
            }
            else
            {
                // Backfill CreatedDate for admins seeded before that field was set
                // (pre-fix runs left it at DateTime.MinValue / 0001-01-01).
                if (existing.CreatedDate == default)
                {
                    existing.CreatedDate = DateTime.UtcNow;
                    await userManager.UpdateAsync(existing);
                }
            }
            return;
        }

        AppUser admin = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            // AppUser is IdentityUser-based, not Entity<TId>, so the SaveChanges
            // audit hook doesn't fire — stamp creation time explicitly.
            CreatedDate = DateTime.UtcNow
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
