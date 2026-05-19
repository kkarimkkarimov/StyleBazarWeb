using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.DTOs.Subscriptions;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Services.Subscriptions;

public class SubscriptionManager : ISubscriptionService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubscriptionManager(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ====== Read ======

    public async Task<SubscriptionDto> GetCurrentForUserAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
            return SynthesizedFree(string.Empty);

        AppUser? user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
            return SynthesizedFree(userId);

        // ---- Lazy expiry: if denormalized plan says Paid but ExpiresAt is past, downgrade now ----
        if (user.CurrentPlan != SubscriptionPlan.Free &&
            user.PlanExpiresAt.HasValue &&
            user.PlanExpiresAt.Value <= DateTime.UtcNow)
        {
            await LazyDowngradeAsync(user, ct);
        }

        // ---- Find the active row for the (possibly just-flipped) state ----
        Subscription? active = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.CreatedDate)
            .FirstOrDefaultAsync(ct);

        if (active is null)
        {
            // No history yet — return synthesized Free reflecting AppUser denormalized state
            return new SubscriptionDto
            {
                Id = 0,
                UserId = userId,
                Plan = user.CurrentPlan,
                Status = SubscriptionStatus.Active,
                StartedAt = user.CreatedDate,
                ExpiresAt = user.PlanExpiresAt,
                CreatedDate = user.CreatedDate
            };
        }

        return await MapAsync(active, ct);
    }

    public Task<SubscriptionDto> GetCurrentForMeAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return Task.FromResult(SynthesizedFree(string.Empty));
        return GetCurrentForUserAsync(_currentUser.UserId, ct);
    }

    public async Task<List<SubscriptionDto>> GetHistoryAsync(string userId, int take = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return new List<SubscriptionDto>();
        if (take < 1) take = 1;
        if (take > 100) take = 100;

        List<Subscription> rows = await _db.Subscriptions
            .AsNoTracking()
            .Include(s => s.ApprovedByAdmin)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedDate)
            .Take(take)
            .ToListAsync(ct);

        return rows.Select(r => new SubscriptionDto
        {
            Id = r.Id,
            UserId = r.UserId,
            Plan = r.Plan,
            Status = r.Status,
            StartedAt = r.StartedAt,
            ExpiresAt = r.ExpiresAt,
            ApprovedByAdminId = r.ApprovedByAdminId,
            ApprovedByAdminFullName = r.ApprovedByAdmin?.FullName,
            Notes = r.Notes,
            CreatedDate = r.CreatedDate
        }).ToList();
    }

    // ====== Write ======

    public async Task<SubscriptionDto> ChangePlanAsync(UpgradeSubscriptionDto dto, string adminId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(dto.UserId))
            throw new BusinessException("UserId tələb olunur.");

        AppUser? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId, ct);
        if (user is null)
            throw new NotFoundException("İstifadəçi tapılmadı.");

        // ExpiresAt validation: only Paid plans get an expiry; Free downgrades clear it
        DateTime? expiresAt = dto.Plan == SubscriptionPlan.Free ? null : dto.ExpiresAt;
        if (dto.Plan != SubscriptionPlan.Free && expiresAt != null && expiresAt <= DateTime.UtcNow)
            throw new BusinessException("Bitmə tarixi gələcəkdə olmalıdır.");

        // 1. Flip any prior Active row → Cancelled
        List<Subscription> activeRows = await _db.Subscriptions
            .Where(s => s.UserId == dto.UserId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(ct);
        foreach (Subscription prior in activeRows)
        {
            prior.Status = SubscriptionStatus.Cancelled;
        }

        // 2. Insert the new Active row
        var fresh = new Subscription
        {
            UserId = dto.UserId,
            Plan = dto.Plan,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            ApprovedByAdminId = adminId,
            Notes = dto.Notes?.Trim()
        };
        _db.Subscriptions.Add(fresh);

        // 3. Update AppUser denormalized fields
        user.CurrentPlan = dto.Plan;
        user.PlanExpiresAt = expiresAt;

        await _db.SaveChangesAsync(ct);

        return await MapAsync(fresh, ct);
    }

    // ====== Quotas ======

    public int GetProductLimit(SubscriptionPlan plan) => PlanCatalog.MaxProductsFor(plan);

    public Task<QuotaCheckResult> CanCurrentUserAddProductAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return Task.FromResult(new QuotaCheckResult(false, SubscriptionPlan.Free, 0, 0, "Sistemə daxil ol."));

        return CanUserAddProductAsync(_currentUser.UserId, ct);
    }

    public async Task<QuotaCheckResult> CanUserAddProductAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId))
            return new QuotaCheckResult(false, SubscriptionPlan.Free, 0, 0, "İstifadəçi tapılmadı.");

        SubscriptionDto current = await GetCurrentForUserAsync(userId, ct);
        int max = GetProductLimit(current.Plan);

        // Count ALL of the user's products across ALL their stores (active+inactive,
        // soft-delete-excluded by global query filter). Quota applies to total slots,
        // not just active ones, so users can't "save" by archiving.
        int count = await _db.Products
            .Where(p => p.Store.OwnerId == userId)
            .CountAsync(ct);

        if (count < max)
            return new QuotaCheckResult(true, current.Plan, count, max, null);

        string upgradeHint = current.Plan switch
        {
            SubscriptionPlan.Free => "Pro plana keç (50 məhsul) və ya VIP (500 məhsul).",
            SubscriptionPlan.Pro => "VIP plana keç (500 məhsul).",
            _ => "Daha çox yer üçün dəstəklə əlaqə saxla."
        };

        return new QuotaCheckResult(false, current.Plan, count, max,
            $"{current.Plan} planında məhsul limiti dolub ({count}/{max}). {upgradeHint}");
    }

    // ====== Internals ======

    private async Task LazyDowngradeAsync(AppUser tracked, CancellationToken ct)
    {
        // Mark the current Active row as Expired (history preserved)
        List<Subscription> activeRows = await _db.Subscriptions
            .Where(s => s.UserId == tracked.Id && s.Status == SubscriptionStatus.Active)
            .ToListAsync(ct);
        foreach (Subscription prior in activeRows)
        {
            prior.Status = SubscriptionStatus.Expired;
        }

        // Insert a Free transition row so the audit log shows when the downgrade happened
        _db.Subscriptions.Add(new Subscription
        {
            UserId = tracked.Id,
            Plan = SubscriptionPlan.Free,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow,
            ExpiresAt = null,
            ApprovedByAdminId = null, // system, not admin
            Notes = "Lazy expiry — auto-downgrade to Free"
        });

        // Update denormalized fields
        tracked.CurrentPlan = SubscriptionPlan.Free;
        tracked.PlanExpiresAt = null;

        await _db.SaveChangesAsync(ct);
    }

    private static SubscriptionDto SynthesizedFree(string userId) => new()
    {
        Id = 0,
        UserId = userId,
        Plan = SubscriptionPlan.Free,
        Status = SubscriptionStatus.Active,
        StartedAt = DateTime.UtcNow,
        ExpiresAt = null,
        CreatedDate = DateTime.UtcNow
    };

    private async Task<SubscriptionDto> MapAsync(Subscription s, CancellationToken ct)
    {
        string? adminName = null;
        if (!string.IsNullOrEmpty(s.ApprovedByAdminId))
        {
            adminName = await _db.Users.AsNoTracking()
                .Where(u => u.Id == s.ApprovedByAdminId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);
        }

        return new SubscriptionDto
        {
            Id = s.Id,
            UserId = s.UserId,
            Plan = s.Plan,
            Status = s.Status,
            StartedAt = s.StartedAt,
            ExpiresAt = s.ExpiresAt,
            ApprovedByAdminId = s.ApprovedByAdminId,
            ApprovedByAdminFullName = adminName,
            Notes = s.Notes,
            CreatedDate = s.CreatedDate
        };
    }
}
