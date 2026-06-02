using Stylebazar.BLL.DTOs.Wishlist;

namespace Stylebazar.BLL.Services.Wishlist;

// IWishlistService — bu xidmətin MÜQAVİLƏSİ (interfeys). Hansı əməliyyatların mövcud olacağını təyin edir; gerçəkləşmə ayrı sinifdədir.
public interface IWishlistService
{
    Task<List<WishlistItemDto>> GetMineAsync(CancellationToken ct = default);
    Task<WishlistItemDto> AddAsync(int productId, CancellationToken ct = default);
    Task RemoveAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Returns the set of product IDs the current user has wishlisted. Used by
    /// list pages to paint heart icons without N+1 lookups. Returns an empty
    /// set for anonymous callers (no exception).
    /// </summary>
    Task<HashSet<int>> GetMyProductIdsAsync(CancellationToken ct = default);

    /// <summary>Total count of items in the current user's wishlist (0 for anon).</summary>
    Task<int> GetMyCountAsync(CancellationToken ct = default);
}
