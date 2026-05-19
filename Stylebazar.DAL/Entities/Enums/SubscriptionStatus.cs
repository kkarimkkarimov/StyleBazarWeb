namespace Stylebazar.DAL.Entities.Enums;

/// <summary>
/// Lifecycle of a single Subscription row. A user always has at most one
/// row with Status = Active at any moment.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Currently valid (between StartedAt and ExpiresAt).</summary>
    Active = 0,

    /// <summary>Hit the ExpiresAt cutoff. Set lazily on next /Seller/* access.</summary>
    Expired = 1,

    /// <summary>Admin or user manually ended the plan before expiry.</summary>
    Cancelled = 2
}
