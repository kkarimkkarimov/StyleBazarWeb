namespace Stylebazar.BLL.DTOs.Categories;

// CategoryListQueryDto — siyahı üçün filtr / səhifələmə parametrlərini daşıyan sorğu DTO-su.
public class CategoryListQueryDto
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public int Page { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? 20 : value;
    }

    /// <summary>
    /// Filter by parent category id. Pass 0 or omit to get all.
    /// </summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>
    /// Case-insensitive name contains.
    /// </summary>
    public string? Search { get; set; }
}
