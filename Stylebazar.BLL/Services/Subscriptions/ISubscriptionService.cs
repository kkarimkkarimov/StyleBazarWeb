using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Subscriptions;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Services.Subscriptions;

// ISubscriptionService — bu xidmətin MÜQAVİLƏSİ (interfeys). Hansı əməliyyatların mövcud olacağını təyin edir; gerçəkləşmə ayrı sinifdədir.
public interface ISubscriptionService
{
    /// <summary>
    /// Current active subscription for the given user. Returns a synthesized
    /// Free DTO (Id=0) when the user has never been upgraded. Also performs
    /// lazy expiry: if the persisted plan has lapsed, transitions the user
    /// back to Free BEFORE returning, so callers always see truth.
    /// </summary>
    Task<SubscriptionDto> GetCurrentForUserAsync(string userId, CancellationToken ct = default);

    /// <summary>Convenience wrapper for ICurrentUserService.UserId.</summary>
    Task<SubscriptionDto> GetCurrentForMeAsync(CancellationToken ct = default);

    /// <summary>Most-recent N subscription rows for the admin User Detail history table.</summary>
    Task<List<SubscriptionDto>> GetHistoryAsync(string userId, int take = 10, CancellationToken ct = default);

    /// <summary>
    /// Admin üçün BÜTÜN abunəlik/ödəniş sətirləri (səhifələnmiş, plan/status filtrli, yeni→köhnə).
    /// Qlobal görünüş — kim, hansı plan, status, tarixlər, mənbə (Stripe ref / admin qeydi).
    /// </summary>
    Task<Paginate<AdminSubscriptionRowDto>> GetAllForAdminAsync(
        SubscriptionPlan? plan, SubscriptionStatus? status, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Admin-only: upgrade or downgrade a user to a specific plan. Atomically:
    ///   1. Flips any prior Active row → Cancelled
    ///   2. Inserts a new Subscription row with Status=Active
    ///   3. Updates AppUser.CurrentPlan + PlanExpiresAt denormalized fields
    /// </summary>
    Task<SubscriptionDto> ChangePlanAsync(UpgradeSubscriptionDto dto, string adminId, CancellationToken ct = default);

    /// <summary>
    /// Stripe ödənişi təsdiqləndikdən SONRA planı tətbiq edir (N ay üçün).
    /// İdempotentdir: eyni paymentReference ilə təkrar çağırış yeni dəyişiklik etmir
    /// (Stripe webhook + success səhifəsi eyni ödənişi iki dəfə tətbiq etməsin deyə).
    /// Eyni planı uzadırsa qalan vaxtın üstünə əlavə edir; fərqli plana keçirsə indidən başlayır.
    /// </summary>
    Task<SubscriptionDto> ApplyPaidPlanAsync(
        string userId, SubscriptionPlan plan, int months, string paymentReference, CancellationToken ct = default);

    /// <summary>
    /// Static cap lookup — does not hit the DB. Used by ProductBusinessRules
    /// after the caller already knows the user's plan.
    /// </summary>
    int GetProductLimit(SubscriptionPlan plan);

    /// <summary>
    /// Combined plan resolution + quota check for the current user. Returns
    /// (ok=true, plan, current, max) when the user can still add a product,
    /// or (ok=false, ...) with reasonMessage in Az for the rejection UI.
    /// </summary>
    Task<QuotaCheckResult> CanCurrentUserAddProductAsync(CancellationToken ct = default);

    /// <summary>
    /// Quota check scoped to an explicit user (the STORE OWNER, not whoever
    /// is currently signed in). Called by ProductManager.CreateAsync so the
    /// gate works for both seller self-uploads and admin-on-behalf creates:
    /// the seller's plan limit is what matters, regardless of who clicked.
    /// </summary>
    Task<QuotaCheckResult> CanUserAddProductAsync(string userId, CancellationToken ct = default);
}

public record QuotaCheckResult(
    bool Ok,
    SubscriptionPlan Plan,
    int CurrentProductCount,
    int MaxAllowed,
    string? ReasonMessage);
