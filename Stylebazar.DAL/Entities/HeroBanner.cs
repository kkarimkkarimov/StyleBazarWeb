// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// ANA SƏHİFƏ BANNERİ entity-si — "HeroBanners" cədvəli. Admin tərəfindən idarə olunur.
// Bir neçə banner DisplayOrder-ə görə növbə ilə göstərilə bilər (slider).
public class HeroBanner : Entity<int>
{
    // Əsas başlıq. Məcburidir.
    public required string Title { get; set; }
    // Alt başlıq. Opsionaldır.
    public string? Subtitle { get; set; }
    // Başlıq üstündəki kiçik mətn (eyebrow). Opsionaldır.
    public string? Eyebrow { get; set; }
    // Banner şəkli. Opsionaldır.
    public string? ImageUrl { get; set; }
    // Düymə mətni (məs. "İndi al"). Opsionaldır.
    public string? CtaText { get; set; }
    // Düymənin keçid ünvanı (URL). Opsionaldır.
    public string? CtaUrl { get; set; }
    // Göstərilmə sırası — kiçik rəqəm öndə.
    public int DisplayOrder { get; set; }
    // Aktivdirmi? Default = true (göstərilir).
    public bool IsActive { get; set; } = true;
    // Göstərilməyə başlama tarixi (planlaşdırma üçün). null = dərhal.
    public DateTime? StartsAt { get; set; }
    // Göstərilmənin bitmə tarixi. null = bitmə yoxdur.
    public DateTime? EndsAt { get; set; }
}
