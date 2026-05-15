namespace Stylebazar.BLL.DTOs.Stores;

public class StoreListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>Short bio shown under the shop name on the Shops page card.</summary>
    public string? Description { get; set; }

    public string ProfileImageUrl { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }

    public bool IsActive { get; set; }
    public int ProductCount { get; set; }

    /// <summary>Up to 5 thumbnails from this store's most-recent products — drives the side-rail on the Shops page card.</summary>
    public List<string> ThumbnailImageUrls { get; set; } = new();

    public DateTime CreatedDate { get; set; }
}
