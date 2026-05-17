using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// Junction table: a buyer (AppUser) follows a Store. The unique index on
/// (UserId, StoreId) in <c>StoreFollowerConfiguration</c> guarantees a single
/// row per pair. Soft-delete is left enabled via Entity&lt;TId&gt; so unfollow
/// semantics could later be "hide" instead of "delete" — today we delete
/// permanently to keep the count accurate without an extra WHERE.
/// </summary>
public class StoreFollower : Entity<int>
{
    public required string UserId { get; set; }
    public int StoreId { get; set; }

    public virtual AppUser User { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
}
