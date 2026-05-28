using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// Instagram-style ephemeral content. Sellers publish per-store stories.
/// Default lifetime = 24h via ExpiresAt; IsActive can be flipped by admin
/// for moderation without waiting on expiry.
///
/// Phase 14.1: now supports both image and video. MediaUrl is the new
/// canonical field; ImageUrl is kept as a property alias for back-compat
/// with existing rows + the AutoMapper profile (maps to/from MediaUrl).
/// </summary>
public class Story : Entity<int>
{
    public int StoreId { get; set; }

    /// <summary>URL to the uploaded image or video file.</summary>
    public required string MediaUrl { get; set; }

    /// <summary>Image vs. Video — drives viewer rendering + upload validation.</summary>
    public StoryMediaType MediaType { get; set; } = StoryMediaType.Image;

    /// <summary>Optional poster frame for videos (auto-set client-side later).</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Video duration in seconds; null for images. Used by the viewer so it
    /// doesn't auto-advance mid-video, and by the validator (max 30s).
    /// </summary>
    public int? DurationSeconds { get; set; }

    public string? Caption { get; set; }
    public string? LinkUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int ViewCount { get; set; }

    public virtual Store Store { get; set; } = null!;
}
