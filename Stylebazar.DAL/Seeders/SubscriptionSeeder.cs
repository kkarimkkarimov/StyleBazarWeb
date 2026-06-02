using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Seeders;

// ABUNƏLİK SEEDER — heç bir abunəlik sətri olmayan KÖHNƏ istifadəçilərə bir ədəd
// "Free" abunəlik sətri əlavə edir (backfill = geriyə doğru doldurma). Beləliklə hər
// istifadəçinin abunəlik tarixçəsi tam olur. İdempotentdir: yalnız sətri OLMAYAN istifadəçilərə əlavə edir.
public static class SubscriptionSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SubscriptionSeeder).FullName!);

        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // HEÇ BİR abunəlik sətri olmayan istifadəçilərin id-lərini gətir.
        List<string> usersNeedingBackfill = await db.Users
            .AsNoTracking()
            .Where(u => !db.Subscriptions.Any(s => s.UserId == u.Id))   // abunəliyi olmayanlar
            .Select(u => u.Id)
            .ToListAsync();

        if (usersNeedingBackfill.Count == 0)
        {
            logger.LogInformation("SubscriptionSeeder: no users to backfill.");
            return;   // hər kəsdə var — iş yoxdur
        }

        DateTime now = DateTime.UtcNow;
        int seeded = 0;

        // Düzgün tarixçə üçün hər istifadəçinin qeydiyyat tarixini bir sorğuda gətir.
        Dictionary<string, DateTime> createdDates = await db.Users
            .AsNoTracking()
            .Where(u => usersNeedingBackfill.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.CreatedDate);

        // Hər istifadəçiyə bir "Free / Active" abunəlik sətri yarat.
        foreach (string userId in usersNeedingBackfill)
        {
            DateTime startedAt = createdDates.TryGetValue(userId, out DateTime created) ? created : now;

            db.Subscriptions.Add(new Subscription
            {
                UserId = userId,
                Plan = SubscriptionPlan.Free,
                Status = SubscriptionStatus.Active,
                StartedAt = startedAt,         // tarixçə dəqiq olsun deyə qeydiyyat tarixi
                ExpiresAt = null,              // Free = ömürlük
                ApprovedByAdminId = null,      // sistem tərəfindən, admin yox
                Notes = "Auto-backfilled at Phase 11 deploy"
            });
            seeded++;
        }

        await db.SaveChangesAsync();   // hamısını birdən bazaya yaz
        logger.LogInformation("SubscriptionSeeder: backfilled {Count} user(s) with Free subscription rows.", seeded);
    }
}
