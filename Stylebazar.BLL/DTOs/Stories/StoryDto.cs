namespace Stylebazar.BLL.DTOs.Stories;

public class StoryDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? LinkUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ViewCount { get; set; }
}
