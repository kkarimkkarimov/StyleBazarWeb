namespace Stylebazar.BLL.Services.StoreFollowers;

/// <summary>
/// Buyer-side "follow this store" subscription. Powers the Follow button on
/// the public store page and the (future) feed of new products from
/// followed stores. Anonymous calls return empty results without throwing
/// so the same code path serves both signed-in and guest users.
/// </summary>
public interface IStoreFollowerService
{
    /// <summary>Returns true after a successful follow (or if already following).</summary>
    Task<FollowResult> FollowAsync(int storeId, CancellationToken ct = default);

    /// <summary>Returns true after a successful unfollow (or if not currently following).</summary>
    Task<FollowResult> UnfollowAsync(int storeId, CancellationToken ct = default);

    /// <summary>Does the current user follow this store? Anonymous → false.</summary>
    Task<bool> IsFollowedByCurrentUserAsync(int storeId, CancellationToken ct = default);

    /// <summary>Distinct follower count for the store.</summary>
    Task<int> GetFollowerCountAsync(int storeId, CancellationToken ct = default);
}

/// <summary>Lightweight result so callers don't import IdentityResult here.</summary>
public class FollowResult
{
    public bool IsFollowing { get; init; }
    public int FollowerCount { get; init; }
}
