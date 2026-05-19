using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Subscriptions;

/// <summary>
/// Posted from Admin/User/Detail when the admin manually upgrades or
/// downgrades a user's plan after receiving payment (or revoking it).
/// </summary>
public class UpgradeSubscriptionDto
{
    public string UserId { get; set; } = string.Empty;
    public SubscriptionPlan Plan { get; set; }

    /// <summary>UTC. Null = no expiry (typical for Free downgrades).</summary>
    public DateTime? ExpiresAt { get; set; }

    public string? Notes { get; set; }
}
