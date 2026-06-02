using Stylebazar.BLL.DTOs.HeroBanners;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Stories;

namespace Stylebazar.MVC.ViewModels;

// HomeIndexViewModel — view (səhifə) üçün məlumat modeli. Controller doldurur, Razor səhifəsi göstərir.
public class HomeIndexViewModel
{
    public List<HeroBannerDto> HeroBanners { get; set; } = new();
    public List<StoreStoriesDto> Stories { get; set; } = new();
    public List<ProductListDto> FeaturedProducts { get; set; } = new();
    public List<ProductListDto> LatestProducts { get; set; } = new();

    /// <summary>Product IDs the current user has wishlisted. Empty for anon.</summary>
    public HashSet<int> WishlistProductIds { get; set; } = new();
}
