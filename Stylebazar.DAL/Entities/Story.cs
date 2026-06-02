using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

// HEKAYƏ (STORY) entity-si — "Stories" cədvəli. Instagram tipli müvəqqəti məzmun.
// Satıcı mağaza adından hekayə paylaşır. Default ömrü = 24 saat (ExpiresAt).
// Admin moderasiya üçün ExpiresAt-ı gözləmədən IsActive-i söndürə bilər. Şəkil + video dəstəklənir.
public class Story : Entity<int>
{
    public int StoreId { get; set; }                  // Hansı mağazaya aiddir (FK → Store)

    public required string MediaUrl { get; set; }      // Yüklənmiş şəkil/video faylının URL-i

    public StoryMediaType MediaType { get; set; } = StoryMediaType.Image;  // Şəkil yoxsa Video (default: Şəkil)

    public string? ThumbnailUrl { get; set; }          // Video üçün üz qabığı (poster) şəkli

    // Video uzunluğu (saniyə); şəkillər üçün null. Viewer videonu yarıda kəsməsin və
    // validator (maks. 30 san) yoxlasın deyə lazımdır.
    public int? DurationSeconds { get; set; }

    public string? Caption { get; set; }               // Altyazı/mətn
    public string? LinkUrl { get; set; }               // Hekayədəki keçid ünvanı
    public DateTime ExpiresAt { get; set; }            // Bitmə vaxtı (adətən +24 saat)
    public bool IsActive { get; set; } = true;         // Aktivdir? (default: bəli)
    public int ViewCount { get; set; }                 // Baxış sayı

    public virtual Store Store { get; set; } = null!;  // Aid olduğu mağaza
}
