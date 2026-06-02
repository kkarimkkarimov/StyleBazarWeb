using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Seeders;

// ROL SEEDER — tətbiq ilk dəfə başlayanda sistemə lazım olan rolları (Admin, Seller, Buyer)
// bazada yaradır. "Seed" = ilkin/zəruri məlumatı bazaya "əkmək". İdempotentdir: rol artıq varsa atlanır.
public static class RoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();   // xidmətlər üçün müvəqqəti scope
        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RoleSeeder).FullName!);

        // Baza əlçatan olana qədər gözlə (LocalDB / SQL Server soyuq başlayanda lazım olur).
        await WaitForDatabaseAsync(scope.ServiceProvider, logger);

        // RoleManager — Identity-nin rol idarəetmə xidməti.
        RoleManager<AppRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        // UserRole enum-undakı hər dəyər (Admin, Seller, Buyer) üçün rol yarat.
        foreach (UserRole role in Enum.GetValues<UserRole>())
        {
            string roleName = role.ToString();
            if (await roleManager.RoleExistsAsync(roleName))
                continue;   // artıq varsa atla (idempotentlik)

            await roleManager.CreateAsync(new AppRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = roleName
            });
        }
    }

    // Baza əlçatan olana qədər təkrar-təkrar yoxlayır (maks. 10 cəhd, hər 2 saniyədən bir).
    private static async Task WaitForDatabaseAsync(IServiceProvider provider, ILogger logger)
    {
        const int maxAttempts = 10;
        TimeSpan delay = TimeSpan.FromSeconds(2);

        ApplicationDbContext db = provider.GetRequiredService<ApplicationDbContext>();

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (await db.Database.CanConnectAsync())   // bazaya qoşula bilirikmi?
                    return;

                logger.LogWarning("Database not yet reachable (attempt {Attempt}/{Max}).", attempt, maxAttempts);
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex, "Database connection attempt {Attempt}/{Max} failed; retrying in {Delay}s.",
                    attempt, maxAttempts, delay.TotalSeconds);
            }

            await Task.Delay(delay);   // gözlə və yenidən cəhd et
        }

        // 10 cəhddən sonra hələ də əlçatan deyilsə — aydın xəta ver.
        throw new InvalidOperationException(
            $"Database was not reachable after {maxAttempts} attempts. Make sure LocalDB / SQL Server is running.");
    }
}
