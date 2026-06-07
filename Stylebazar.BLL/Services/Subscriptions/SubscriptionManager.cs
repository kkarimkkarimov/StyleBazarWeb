using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.DTOs.Subscriptions;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Subscriptions;

// SubscriptionManager — xidmətin GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı). Yoxlama → repository/baza → nəticə axını burada idarə olunur.
public class SubscriptionManager : ISubscriptionService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly IValidator<UpgradeSubscriptionDto> _upgradeValidator;

    public SubscriptionManager(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IMapper mapper,
        IValidator<UpgradeSubscriptionDto> upgradeValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _mapper = mapper;
        _upgradeValidator = upgradeValidator;
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

        return _mapper.Map<List<SubscriptionDto>>(rows);
    }

    // Admin qlobal siyahısı — bütün abunəlik/ödəniş sətirləri (istifadəçi məlumatı ilə birgə).
    public async Task<Paginate<AdminSubscriptionRowDto>> GetAllForAdminAsync(
        SubscriptionPlan? plan, SubscriptionStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        IQueryable<Subscription> q = _db.Subscriptions.AsNoTracking();
        if (plan.HasValue) q = q.Where(s => s.Plan == plan.Value);
        if (status.HasValue) q = q.Where(s => s.Status == status.Value);

        int count = await q.CountAsync(ct);

        // Birbaşa DTO-ya proyeksiya — istifadəçi adı/email + təsdiqləyən admin JOIN ilə gəlir.
        List<AdminSubscriptionRowDto> items = await q
            .OrderByDescending(s => s.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new AdminSubscriptionRowDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserFullName = s.User.FullName,
                UserEmail = s.User.Email ?? string.Empty,
                Plan = s.Plan,
                Status = s.Status,
                StartedAt = s.StartedAt,
                ExpiresAt = s.ExpiresAt,
                Notes = s.Notes,
                ApprovedByAdminFullName = s.ApprovedByAdmin != null ? s.ApprovedByAdmin.FullName : null,
                CreatedDate = s.CreatedDate
            })
            .ToListAsync(ct);

        return new Paginate<AdminSubscriptionRowDto>
        {
            Index = page - 1,
            Size = pageSize,
            Count = count,
            Pages = pageSize > 0 ? (int)Math.Ceiling(count / (double)pageSize) : 0,
            Items = items
        };
    }

    // ====== Write ======

    public async Task<SubscriptionDto> ChangePlanAsync(UpgradeSubscriptionDto dto, string adminId, CancellationToken ct = default)
    {
        // FluentValidator handles required/enum-range/expiry-future/notes-length;
        // anything below is business logic that needs DB state to evaluate.
        var validation = await _upgradeValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        AppUser? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId, ct);
        if (user is null)
            throw new NotFoundException("İstifadəçi tapılmadı.");

        // Free downgrades always clear ExpiresAt regardless of what the form sent.
        DateTime? expiresAt = dto.Plan == SubscriptionPlan.Free ? null : dto.ExpiresAt;

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

    // Stripe ödənişi təsdiqlənəndən sonra planı N ay üçün tətbiq edir (admin yox — sistem/Stripe).
    public async Task<SubscriptionDto> ApplyPaidPlanAsync(
        string userId, SubscriptionPlan plan, int months, string paymentReference, CancellationToken ct = default)
    {
        if (plan == SubscriptionPlan.Free)
            throw new BusinessException("Pulsuz plan ödənişlə tətbiq olunmur.");
        if (months < 1) months = 1;

        AppUser? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            throw new NotFoundException("İstifadəçi tapılmadı.");

        // İDEMPOTENTLİK: bu ödəniş artıq tətbiq olunubsa (Notes-da ref var), heç nə etmə —
        // mövcud aktiv abunəliyi qaytar. (Webhook + success səhifəsi eyni ödənişi göndərə bilər.)
        if (!string.IsNullOrEmpty(paymentReference))
        {
            bool already = await _db.Subscriptions.AsNoTracking()
                .AnyAsync(s => s.UserId == userId && s.Notes != null && s.Notes.Contains(paymentReference), ct);
            if (already)
                return await GetCurrentForUserAsync(userId, ct);
        }

        DateTime now = DateTime.UtcNow;
        // Eyni planı uzadırsa qalan vaxtın üstünə əlavə et; fərqli plana keçirsə indidən başla.
        DateTime baseDate = (user.CurrentPlan == plan && user.PlanExpiresAt.HasValue && user.PlanExpiresAt.Value > now)
            ? user.PlanExpiresAt.Value
            : now;
        DateTime expiresAt = baseDate.AddMonths(months);

        // 1. Köhnə aktiv sətirləri bağla
        List<Subscription> activeRows = await _db.Subscriptions
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(ct);
        foreach (Subscription prior in activeRows)
            prior.Status = SubscriptionStatus.Cancelled;

        // 2. Yeni aktiv sətir əlavə et (audit + idempotentlik üçün ödəniş ref-i Notes-da)
        var fresh = new Subscription
        {
            UserId = userId,
            Plan = plan,
            Status = SubscriptionStatus.Active,
            StartedAt = now,
            ExpiresAt = expiresAt,
            ApprovedByAdminId = null,                 // sistem/Stripe, admin yox
            Notes = $"Stripe ödənişi · {months} ay · ref: {paymentReference}"
        };
        _db.Subscriptions.Add(fresh);

        // 3. AppUser denormalizasiya sahələrini yenilə
        user.CurrentPlan = plan;
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
        // AutoMapper projects everything except ApprovedByAdminFullName,
        // which depends on the navigation property being loaded. Load it
        // here only when the row was added in-memory without Include().
        SubscriptionDto dto = _mapper.Map<SubscriptionDto>(s);
        if (string.IsNullOrEmpty(dto.ApprovedByAdminFullName) && !string.IsNullOrEmpty(s.ApprovedByAdminId))
        {
            dto.ApprovedByAdminFullName = await _db.Users.AsNoTracking()
                .Where(u => u.Id == s.ApprovedByAdminId)
                .Select(u => u.FullName)
                .FirstOrDefaultAsync(ct);
        }
        return dto;
    }
}
