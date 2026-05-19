using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Addresses;
using Stylebazar.BLL.DTOs.Analytics;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Services.Reviews;

namespace Stylebazar.MVC.ViewModels;

/// <summary>
/// Shared envelope for every page inside /Seller/* (image 2 Seller Panel layout).
/// </summary>
public class SellerPanelViewModel
{
    public AccountProfileDto Profile { get; set; } = new();

    /// <summary>The seller's primary store, if any (used for vendor code, social media, etc.).</summary>
    public StoreDetailDto? PrimaryStore { get; set; }
}

public class SellerProfilePageViewModel : SellerPanelViewModel
{
    public List<ActiveSessionDto> ActiveSessions { get; set; } = new();
    public List<LoginEventDto> LoginHistory { get; set; } = new();
    public List<AddressDto> Addresses { get; set; } = new();
}

public class SellerReviewsPageViewModel : SellerPanelViewModel
{
    public List<SellerReviewRowDto> Reviews { get; set; } = new();
    public double AverageRating { get; set; }
    public int TotalCount { get; set; }
    public int FiveStarCount { get; set; }
    public int LowStarCount { get; set; }   // 1–2 stars — needs attention
    public int? MaxRatingFilter { get; set; }
}

public class SellerAnalyticsPageViewModel : SellerPanelViewModel
{
    public SellerAnalyticsDto Stats { get; set; } = new();
}

public class SellerPackagesPageViewModel : SellerPanelViewModel
{
    public Stylebazar.BLL.DTOs.Subscriptions.SubscriptionDto CurrentSubscription { get; set; } = new();
    public IReadOnlyList<Stylebazar.BLL.DTOs.Subscriptions.PlanInfoDto> Tiers { get; set; } = Array.Empty<Stylebazar.BLL.DTOs.Subscriptions.PlanInfoDto>();

    /// <summary>Total products owned across all stores (drives "4 / 5 məhsul" line).</summary>
    public int CurrentProductCount { get; set; }
}

public class SellerDashboardPageViewModel : SellerPanelViewModel
{
    public Stylebazar.BLL.DTOs.Subscriptions.SubscriptionDto CurrentSubscription { get; set; } = new();
    public int ProductCount { get; set; }
    public int ProductLimit { get; set; }

    /// <summary>Aggregated analytics — only meaningful for Pro/VIP, but rendered as zeros for Free too.</summary>
    public Stylebazar.BLL.DTOs.Analytics.SellerAnalyticsDto Stats { get; set; } = new();

    /// <summary>Last 5 reviews on seller's products — drives the "Yeni rəylər" feed card.</summary>
    public List<Stylebazar.BLL.Services.Reviews.SellerReviewRowDto> RecentReviews { get; set; } = new();

    /// <summary>True if the seller hasn't created any store yet — drives the empty-state CTA.</summary>
    public bool HasStore { get; set; }
}
