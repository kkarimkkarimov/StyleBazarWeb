using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Subscriptions;

/// <summary>
/// Static tier metadata — what each plan costs, what features it
/// unlocks, what the product cap is. Used by /Seller/Packages to
/// render the 3-column pricing comparison and by ProductManager to
/// gate creation.
///
/// Kept in a single typed source (PlanCatalog) so feature flags can't
/// drift between the gate and the pricing page.
/// </summary>
public class PlanInfoDto
{
    public SubscriptionPlan Plan { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public decimal MonthlyPriceAzn { get; set; }
    public int MaxProducts { get; set; }
    public bool HasAnalytics { get; set; }
    public bool HasVerifiedBadge { get; set; }
    public bool HasCatalogPriority { get; set; }
    public List<string> Highlights { get; set; } = new();
}
