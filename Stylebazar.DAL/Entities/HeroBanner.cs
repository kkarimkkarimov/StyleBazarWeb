using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// ANA SƏHİFƏ BANNERİ entity-si — "HeroBanners" cədvəli. Admin tərəfindən idarə olunur.
// Bir neçə banner DisplayOrder-ə görə növbə ilə göstərilə bilər (slider).
public class HeroBanner : Entity<int>
{
    public required string Title { get; set; }     // Əsas başlıq
    public string? Subtitle { get; set; }           // Alt başlıq
    public string? Eyebrow { get; set; }            // Başlıq üstündəki kiçik mətn
    public string? ImageUrl { get; set; }           // Banner şəkli
    public string? CtaText { get; set; }            // Düymə mətni (məs. "İndi al")
    public string? CtaUrl { get; set; }             // Düymənin keçid ünvanı
    public int DisplayOrder { get; set; }           // Göstərilmə sırası
    public bool IsActive { get; set; } = true;      // Aktivdir? (default: bəli)
    public DateTime? StartsAt { get; set; }         // Göstərilməyə başlama tarixi (planlaşdırma)
    public DateTime? EndsAt { get; set; }           // Göstərilmənin bitmə tarixi
}
