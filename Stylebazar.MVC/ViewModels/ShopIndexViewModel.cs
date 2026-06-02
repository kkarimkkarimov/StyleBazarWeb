using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Products;

namespace Stylebazar.MVC.ViewModels;

// ShopIndexViewModel — view (səhifə) üçün məlumat modeli. Controller doldurur, Razor səhifəsi göstərir.
public class ShopIndexViewModel
{
    /// <summary>Current filter state (bound from query string).</summary>
    public ProductListQueryDto Query { get; set; } = new();

    /// <summary>Paginated product results.</summary>
    public Paginate<ProductListDto> Products { get; set; } = new();

    /// <summary>Categories for the filter sidebar (flat, name-sorted).</summary>
    public List<CategoryListDto> Categories { get; set; } = new();

    /// <summary>Distinct brand names found in current product population.</summary>
    public List<string> Brands { get; set; } = new();

    /// <summary>Product IDs the current user has wishlisted. Empty for anon.</summary>
    public HashSet<int> WishlistProductIds { get; set; } = new();
}
