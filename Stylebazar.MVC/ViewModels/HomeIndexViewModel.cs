using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.HeroBanners;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.DTOs.Stories;

namespace Stylebazar.MVC.ViewModels;

public class HomeIndexViewModel
{
    public List<HeroBannerDto> Banners { get; set; } = new();
    public List<StoreStoriesDto> Stories { get; set; } = new();
    public List<CategoryTreeDto> Categories { get; set; } = new();
    public List<ProductListDto> FeaturedProducts { get; set; } = new();
    public List<ProductListDto> LatestProducts { get; set; } = new();

    /// <summary>Top stores showcased on the home page (Featured Shops rail).</summary>
    public List<StoreListDto> FeaturedStores { get; set; } = new();

    /// <summary>Platform stat: total active products on the marketplace.</summary>
    public int TotalProductCount { get; set; }

    /// <summary>Platform stat: total active sellers.</summary>
    public int TotalStoreCount { get; set; }

    /// <summary>Product IDs the current user has wishlisted. Empty for anon.</summary>
    public HashSet<int> WishlistProductIds { get; set; } = new();
}
