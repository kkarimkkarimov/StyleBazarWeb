using Stylebazar.BLL.DTOs.Subscriptions;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Services.Subscriptions;

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
    /// Admin-only: upgrade or downgrade a user to a specific plan. Atomically:
    ///   1. Flips any prior Active row → Cancelled
    ///   2. Inserts a new Subscription row with Status=Active
    ///   3. Updates AppUser.CurrentPlan + PlanExpiresAt denormalized fields
    /// </summary>
    Task<SubscriptionDto> ChangePlanAsync(UpgradeSubscriptionDto dto, string adminId, CancellationToken ct = default);

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
}

public record QuotaCheckResult(
    bool Ok,
    SubscriptionPlan Plan,
    int CurrentProductCount,
    int MaxAllowed,
    string? ReasonMessage);
