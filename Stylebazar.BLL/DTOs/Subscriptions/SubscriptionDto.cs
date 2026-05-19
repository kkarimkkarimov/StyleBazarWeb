using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Subscriptions;

/// <summary>
/// View representation of a single subscription row (history entry) OR
/// a synthesized Free default when the user has never been upgraded.
/// Drives /Seller/Packages "current plan" badge and the admin User
/// Detail subscription history table.
/// </summary>
public class SubscriptionDto
{
    /// <summary>Persisted row id; 0 for the synthesized Free default.</summary>
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public string? ApprovedByAdminId { get; set; }
    public string? ApprovedByAdminFullName { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }

    // ---- Derived helpers consumed by the UI ----

    /// <summary>True when Plan != Free and ExpiresAt &gt; UtcNow (or null = lifetime).</summary>
    public bool IsCurrentlyActive =>
        Status == SubscriptionStatus.Active &&
        Plan != SubscriptionPlan.Free &&
        (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);

    /// <summary>Days remaining until expiry, null if no expiry or already expired.</summary>
    public int? DaysRemaining =>
        ExpiresAt == null ? null
        : ExpiresAt <= DateTime.UtcNow ? 0
        : (int)Math.Ceiling((ExpiresAt.Value - DateTime.UtcNow).TotalDays);
}
