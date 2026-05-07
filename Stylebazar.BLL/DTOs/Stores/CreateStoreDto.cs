namespace Stylebazar.BLL.DTOs.Stores;

public class CreateStoreDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
}
