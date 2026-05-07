namespace Stylebazar.BLL.DTOs.Stores;

public class StoreListQueryDto
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

    /// <summary>
    /// Public listing should set this to true to hide inactive/soft-blocked stores.
    /// Admin listing can leave it null to see everything.
    /// </summary>
    public bool? IsActive { get; set; }

    public string? OwnerId { get; set; }
}
