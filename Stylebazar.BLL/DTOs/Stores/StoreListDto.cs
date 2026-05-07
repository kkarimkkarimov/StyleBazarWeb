namespace Stylebazar.BLL.DTOs.Stores;

public class StoreListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedDate { get; set; }
}
