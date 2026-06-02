namespace Stylebazar.BLL.DTOs.HeroBanners;

// UpdateHeroBannerDto — mövcud qeydi YENİLƏMƏK üçün sahələri daşıyan DTO.
public class UpdateHeroBannerDto
{
    public string Title { get; set; } = string.Empty;
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
