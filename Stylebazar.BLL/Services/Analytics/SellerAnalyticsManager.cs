using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.DTOs.Analytics;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Contexts;

namespace Stylebazar.BLL.Services.Analytics;

// SellerAnalyticsManager — xidmətin GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı). Yoxlama → repository/baza → nəticə axını burada idarə olunur.
public class SellerAnalyticsManager : ISellerAnalyticsService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IContactClickService _clicks;

    public SellerAnalyticsManager(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IContactClickService clicks)
    {
        _db = db;
        _currentUser = currentUser;
        _clicks = clicks;
    }

    public async Task<SellerAnalyticsDto> GetForCurrentSellerAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return new SellerAnalyticsDto();

        string sellerId = _currentUser.UserId;

        // 1. Resolve owned store ids — cheap & cached path
        List<int> storeIds = await _db.Stores
            .AsNoTracking()
            .Where(s => s.OwnerId == sellerId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (storeIds.Count == 0)
        {
            // No stores → reuse empty contact stats (shows zeros consistently)
            var empty = await _clicks.GetStatsForSellerAsync(sellerId, ct);
            return new SellerAnalyticsDto { Contacts = empty };
        }

        // 2. Product counts (active vs all). Single round-trip via grouping.
        var productSummary = await _db.Products
            .AsNoTracking()
            .Where(p => storeIds.Contains(p.StoreId))
            .GroupBy(p => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(p => p.IsActive)
            })
            .FirstOrDefaultAsync(ct);

        int totalProducts = productSummary?.Total ?? 0;
        int activeProducts = productSummary?.Active ?? 0;

        // 3. Followers across all owned stores
        int totalFollowers = await _db.StoreFollowers
            .AsNoTracking()
            .CountAsync(f => storeIds.Contains(f.StoreId), ct);

        // 4. Wishlist adds — count of wishlist rows on the seller's products
        int totalWishlist = await _db.Wishlists
            .AsNoTracking()
            .Where(w => storeIds.Contains(w.Product.StoreId))
            .CountAsync(ct);

        // 5. Reviews + average rating across seller's products
        var reviewSummary = await _db.Reviews
            .AsNoTracking()
            .Where(r => storeIds.Contains(r.Product.StoreId))
            .GroupBy(r => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Avg = g.Average(r => (double?)r.Rating) ?? 0d
            })
            .FirstOrDefaultAsync(ct);

        int totalReviews = reviewSummary?.Count ?? 0;
        double avgRating = reviewSummary != null ? Math.Round(reviewSummary.Avg, 2) : 0d;

        // 6. Contact click stats — already aggregates by store + product + type
        ContactClickStatsDto clicks = await _clicks.GetStatsForSellerAsync(sellerId, ct);

        return new SellerAnalyticsDto
        {
            TotalStores = storeIds.Count,
            TotalProducts = totalProducts,
            TotalActiveProducts = activeProducts,
            TotalFollowers = totalFollowers,
            TotalWishlistAdds = totalWishlist,
            TotalReviews = totalReviews,
            AverageRating = avgRating,
            Contacts = clicks
        };
    }
}
