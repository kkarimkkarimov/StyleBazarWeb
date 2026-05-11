using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// Admin-managed homepage hero banner. Multiple banners can rotate via DisplayOrder.
/// </summary>
public class HeroBanner : Entity<int>
{
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Eyebrow { get; set; }
    public string? ImageUrl { get; set; }
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
}
