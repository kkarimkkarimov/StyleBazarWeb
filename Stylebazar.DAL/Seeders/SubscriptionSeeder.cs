using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Seeders;

/// <summary>
/// Idempotent backfill: ensures every existing AppUser has at least one
/// Subscription history row. Without this, users created before the
/// Phase 11.1 migration look "Free" via the synthesized fallback in
/// SubscriptionManager.GetCurrentForUserAsync, but the audit trail
/// (admin /Admin/User/Detail history table) shows nothing.
///
/// Runs on every app startup right after RoleSeeder + AdminSeeder. Safe
/// to re-execute because:
///   1. We only insert when the user has ZERO subscription rows
///   2. The denormalized AppUser.CurrentPlan is left untouched (admin
///      may have already set it via Phase 11.5 — we don't second-guess)
///   3. StartedAt = user.CreatedDate preserves true chronology
/// </summary>
public static class SubscriptionSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        ILogger logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(SubscriptionSeeder).FullName!);

        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Pull ids of users WITHOUT any subscription row (left-join not-exists)
        List<string> usersNeedingBackfill = await db.Users
            .AsNoTracking()
            .Where(u => !db.Subscriptions.Any(s => s.UserId == u.Id))
            .Select(u => u.Id)
            .ToListAsync();

        if (usersNeedingBackfill.Count == 0)
        {
            logger.LogInformation("SubscriptionSeeder: no users to backfill.");
            return;
        }

        DateTime now = DateTime.UtcNow;
        int seeded = 0;

        // We need the CreatedDate of each user for accurate audit, so fetch
        // the relevant rows in one query (small set — only users with no
        // existing subscription).
        Dictionary<string, DateTime> createdDates = await db.Users
            .AsNoTracking()
            .Where(u => usersNeedingBackfill.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.CreatedDate);

        foreach (string userId in usersNeedingBackfill)
        {
            DateTime startedAt = createdDates.TryGetValue(userId, out DateTime created) ? created : now;

            db.Subscriptions.Add(new Subscription
            {
                UserId = userId,
                Plan = SubscriptionPlan.Free,
                Status = SubscriptionStatus.Active,
                StartedAt = startedAt,
                ExpiresAt = null,
                ApprovedByAdminId = null,  // System-seeded, no human approver
                Notes = "Auto-backfilled at Phase 11 deploy"
            });
            seeded++;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("SubscriptionSeeder: backfilled {Count} user(s) with Free subscription rows.", seeded);
    }
}
