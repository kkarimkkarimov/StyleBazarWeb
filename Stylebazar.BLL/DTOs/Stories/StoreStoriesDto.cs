namespace Stylebazar.BLL.DTOs.Stories;

/// <summary>
/// Grouped feed: one entry per store with all its active stories.
/// Used by the home page Story rail.
/// </summary>
public class StoreStoriesDto
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty;
    public string? StoreProfileImageUrl { get; set; }

    /// <summary>
    /// The store owner's user-id — the recipient when a viewer replies to a
    /// story via the chat composer (Phase 14.7).
    /// </summary>
    public string StoreOwnerId { get; set; } = string.Empty;

    public List<StoryDto> Stories { get; set; } = new();
}
