using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Products;

namespace Stylebazar.MVC.ViewModels;

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
}
