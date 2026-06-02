using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Stories;

// StoryDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class StoryDto
{
    public int Id { get; set; }
    public int StoreId { get; set; }

    /// <summary>Image or video URL (see <see cref="MediaType"/>).</summary>
    public string MediaUrl { get; set; } = string.Empty;

    public StoryMediaType MediaType { get; set; } = StoryMediaType.Image;

    /// <summary>Optional poster frame for videos.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Null for images; used by viewer to fix progress-bar duration.</summary>
    public int? DurationSeconds { get; set; }

    public string? Caption { get; set; }
    public string? LinkUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int ViewCount { get; set; }
}
