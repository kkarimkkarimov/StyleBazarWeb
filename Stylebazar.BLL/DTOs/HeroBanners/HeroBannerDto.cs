namespace Stylebazar.BLL.DTOs.HeroBanners;

// HeroBannerDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class HeroBannerDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Eyebrow { get; set; }
    public string? ImageUrl { get; set; }
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
