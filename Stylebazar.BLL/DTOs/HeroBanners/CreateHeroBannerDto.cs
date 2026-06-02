namespace Stylebazar.BLL.DTOs.HeroBanners;

// CreateHeroBannerDto — yeni qeyd YARATMAQ üçün lazım olan sahələri daşıyan DTO (məlumat ötürmə obyekti).
public class CreateHeroBannerDto
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Eyebrow { get; set; }
    public string? ImageUrl { get; set; }
    public string? CtaText { get; set; }
    public string? CtaUrl { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
}
