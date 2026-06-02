using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Products;

// ProductListQueryDto — siyahı üçün filtr / səhifələmə parametrlərini daşıyan sorğu DTO-su.
public class ProductListQueryDto
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public int Page { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? 20 : value;
    }

    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? StoreId { get; set; }
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsFeatured { get; set; }
    public bool? IsActive { get; set; }

    /// <summary>True → only products with StockQuantity > 0. False/null → no filter.</summary>
    public bool? InStock { get; set; }

    public ProductCondition? Condition { get; set; }

    /// <summary>
    /// price-asc, price-desc, newest (default), name-asc, name-desc
    /// </summary>
    public string? SortBy { get; set; }
}
