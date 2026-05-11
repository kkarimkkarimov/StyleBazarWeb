using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// Instagram-style ephemeral content. Sellers publish per-store stories.
/// Default lifetime = 24h via ExpiresAt; IsActive can be flipped by admin
/// for moderation without waiting on expiry.
/// </summary>
public class Story : Entity<int>
{
    public int StoreId { get; set; }
    public required string ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? LinkUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int ViewCount { get; set; }

    public virtual Store Store { get; set; } = null!;
}
