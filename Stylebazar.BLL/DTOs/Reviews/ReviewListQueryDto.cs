namespace Stylebazar.BLL.DTOs.Reviews;

// ReviewListQueryDto — siyahı üçün filtr / səhifələmə parametrlərini daşıyan sorğu DTO-su.
public class ReviewListQueryDto
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public int Page { get; set; } = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value < 1 ? 20 : value;
    }

    /// <summary>Filter by exact rating (1..5).</summary>
    public int? Rating { get; set; }

    /// <summary>newest (default), oldest, rating-asc, rating-desc</summary>
    public string? SortBy { get; set; }
}
