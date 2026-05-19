using Stylebazar.BLL.DTOs.Subscriptions;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Services.Subscriptions;

/// <summary>
/// Single source of truth for tier metadata. Both the quota gate
/// (ProductManager.CreateAsync) and the pricing page (/Seller/Packages)
/// read from this catalog so feature flags cannot drift between them.
///
/// If a price or limit changes, change it HERE and nowhere else.
/// </summary>
public static class PlanCatalog
{
    public static readonly PlanInfoDto Free = new()
    {
        Plan = SubscriptionPlan.Free,
        Name = "Free",
        Tagline = "Başlanğıc üçün ideal",
        MonthlyPriceAzn = 0m,
        MaxProducts = 5,
        HasAnalytics = false,
        HasVerifiedBadge = false,
        HasCatalogPriority = false,
        Highlights = new List<string>
        {
            "5 məhsul",
            "Mağaza profili + kontakt düymələri",
            "Hekayələr (məhdud)",
            "Wishlist statistikası YOX",
            "Verified badge YOX"
        }
    };

    public static readonly PlanInfoDto Pro = new()
    {
        Plan = SubscriptionPlan.Pro,
        Name = "Pro",
        Tagline = "Aktiv satıcılar üçün",
        MonthlyPriceAzn = 15m,
        MaxProducts = 50,
        HasAnalytics = true,
        HasVerifiedBadge = false,
        HasCatalogPriority = false,
        Highlights = new List<string>
        {
            "50 məhsul",
            "Tam analytics: click, wishlist, top məhsullar",
            "Limitsiz hekayə",
            "Kanal bölgüsü qrafiki",
            "Email dəstək (24 saat)"
        }
    };

    public static readonly PlanInfoDto VIP = new()
    {
        Plan = SubscriptionPlan.VIP,
        Name = "VIP",
        Tagline = "Top mağazalar üçün",
        MonthlyPriceAzn = 49m,
        MaxProducts = 500,
        HasAnalytics = true,
        HasVerifiedBadge = true,
        HasCatalogPriority = true,
        Highlights = new List<string>
        {
            "500 məhsul",
            "Verified ✓ badge (avtomatik)",
            "Mağaza katalogda yuxarıda görünür",
            "Tam analytics + advanced report",
            "Limitsiz hekayə + featured slot",
            "Telefon + email dəstək (priority)"
        }
    };

    /// <summary>3-tier list ordered ascending — drives /Seller/Packages.</summary>
    public static IReadOnlyList<PlanInfoDto> All { get; } = new[] { Free, Pro, VIP };

    /// <summary>Look up tier metadata by enum value. Falls back to Free.</summary>
    public static PlanInfoDto For(SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Pro => Pro,
        SubscriptionPlan.VIP => VIP,
        _ => Free
    };

    /// <summary>Product quota cap (5 / 50 / 500).</summary>
    public static int MaxProductsFor(SubscriptionPlan plan) => For(plan).MaxProducts;
}
