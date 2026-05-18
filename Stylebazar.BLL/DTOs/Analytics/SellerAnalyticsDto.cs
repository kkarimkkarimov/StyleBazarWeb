namespace Stylebazar.BLL.DTOs.Analytics;

/// <summary>
/// Single-call payload for the /Seller/Analytics dashboard. Composes
/// metrics from contact clicks, products, wishlist adds, and reviews
/// so the view doesn't have to fan out N services itself.
/// </summary>
public class SellerAnalyticsDto
{
    public int TotalStores { get; set; }
    public int TotalProducts { get; set; }
    public int TotalActiveProducts { get; set; }
    public int TotalFollowers { get; set; }
    public int TotalWishlistAdds { get; set; }
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }

    /// <summary>Contact click stats — total, 7d, 30d, by channel, top products.</summary>
    public ContactClickStatsDto Contacts { get; set; } = new();
}
