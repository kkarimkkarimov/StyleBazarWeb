using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// Audit row for a single subscription event in a user's history.
/// Hybrid model: AppUser holds the denormalized "current" plan for fast
/// reads; this table is the immutable log of every change so finance
/// and disputes can be reconstructed (e.g. "user says they paid 3 months,
/// not 1 — show me the row admin entered").
///
/// Always exactly ONE row per user has Status = Active. UpgradeAsync
/// flips the prior Active row to Expired/Cancelled before inserting the
/// new Active row.
/// </summary>
public class Subscription : Entity<int>
{
    public required string UserId { get; set; }

    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>UTC. Set to the moment admin applied the change.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>UTC. Null = lifetime (e.g. when admin sets Free as a downgrade).</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>UserId of the admin who approved the change (for audit).</summary>
    public string? ApprovedByAdminId { get; set; }

    /// <summary>Free-form note ("Kapital Bank, 19.05.2026, 3-aylıq Pro").</summary>
    public string? Notes { get; set; }

    public virtual AppUser User { get; set; } = null!;
    public virtual AppUser? ApprovedByAdmin { get; set; }
}
