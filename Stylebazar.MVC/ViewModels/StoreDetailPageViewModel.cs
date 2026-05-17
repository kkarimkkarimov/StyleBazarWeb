using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.DTOs.Stories;

namespace Stylebazar.MVC.ViewModels;

/// <summary>
/// /Store/Detail/{slug} — the public storefront. Mirrors the screenshot:
/// cover + circle avatar + name + description + Follow CTA + social chips
/// + Stories carousel + product grid header + product grid.
/// </summary>
public class StoreDetailPageViewModel
{
    public StoreDetailDto Store { get; set; } = new() { Name = string.Empty, Slug = string.Empty, OwnerId = string.Empty };

    public List<ProductListDto> Products { get; set; } = new();
    public List<StoryDto> Stories { get; set; } = new();

    /// <summary>Total follower count rendered next to the Follow button.</summary>
    public int FollowerCount { get; set; }

    /// <summary>Current user already follows this store? Drives Follow vs Following button label.</summary>
    public bool IsFollowedByCurrentUser { get; set; }

    /// <summary>Wishlist ids for the current user (heart paint on product cards).</summary>
    public HashSet<int> WishlistProductIds { get; set; } = new();

    /// <summary>Selected sort key (newest / price-asc / price-desc).</summary>
    public string Sort { get; set; } = "newest";

    public bool ShowInStockOnly { get; set; }
}
