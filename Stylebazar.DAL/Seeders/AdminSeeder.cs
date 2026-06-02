using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Seeders;

// ADMIN SEEDER — başlanğıcda konfiqurasiyadakı (appsettings/env) məlumatla admin
// istifadəçisini yaradır. Əgər həmin email-li istifadəçi artıq varsa, onu Admin roluna
// yüksəldir. İdempotentdir: dəfələrlə işləsə də təkrar admin yaratmır.
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        IConfiguration config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(AdminSeeder).FullName!);

        // Admin məlumatlarını konfiqurasiyadan oxu (Admin:Email, Admin:FullName, Admin:Password).
        string? email = config["Admin:Email"];
        string? fullName = config["Admin:FullName"];
        string? password = config["Admin:Password"];

        // Hər hansı biri yoxdursa — admin yaratmağı atla.
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Admin seeding skipped: Admin section is not configured.");
            return;
        }

        UserManager<AppUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        // Bu email-li istifadəçi artıq varmı?
        AppUser? existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            bool wasAdmin = await userManager.IsInRoleAsync(existing, UserRole.Admin.ToString());

            // Admin rolunda deyilsə — Admin roluna əlavə et.
            if (!wasAdmin)
                await userManager.AddToRoleAsync(existing, UserRole.Admin.ToString());

            // YALNIZ yüksəltmə anında BİR dəfə parolu sinxronlaşdır. Yəni mövcud bir
            // istifadəçini (məs. əvvəl Buyer olanı) admin edəndə, onun parolunu
            // konfiqdəki parola çevir. Artıq admindirsə bu hissə atlanır ki, sonradan
            // istifadəçinin özü dəyişdiyi parol hər başlanğıcda geri qayıtmasın.
            if (!wasAdmin)
            {
                // Parolu birbaşa təyin etmək olmur — Identity reset token tələb edir.
                string resetToken = await userManager.GeneratePasswordResetTokenAsync(existing);
                await userManager.ResetPasswordAsync(existing, resetToken, password);

                existing.FullName = fullName;
                existing.EmailConfirmed = true;   // admin email-i təsdiqlənmiş sayılır
                if (existing.CreatedDate == default) existing.CreatedDate = DateTime.UtcNow;
                await userManager.UpdateAsync(existing);

                logger.LogInformation(
                    "Existing user '{Email}' promoted to Admin and password synced to Admin:Password config.",
                    email);
            }
            else
            {
                // Köhnə admin-lərdə CreatedDate boş qalıbsa (yamaqdan əvvəl) — doldur.
                if (existing.CreatedDate == default)
                {
                    existing.CreatedDate = DateTime.UtcNow;
                    await userManager.UpdateAsync(existing);
                }
            }
            return;
        }

        // İstifadəçi yoxdursa — yeni admin yarat.
        AppUser admin = new()
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            // AppUser Identity-əsaslıdır (Entity<TId> deyil), ona görə SaveChanges audit
            // hook-u işləmir — yaranma tarixini əl ilə qoyuruq.
            CreatedDate = DateTime.UtcNow
        };

        // İstifadəçini parol ilə yarat (Identity parolu hash-ləyir).
        IdentityResult create = await userManager.CreateAsync(admin, password);
        if (!create.Succeeded)
        {
            logger.LogError("Failed to create admin user: {Errors}",
                string.Join("; ", create.Errors.Select(e => e.Description)));
            return;
        }

        // Admin roluna əlavə et. Alınmasa — yaratdığımız istifadəçini geri sil (təmizlik).
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
