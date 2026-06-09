// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;
// Stylebazar.DAL.Entities.Enums — StoryMediaType enum-unu gətirir.
using Stylebazar.DAL.Entities.Enums;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// HEKAYƏ (STORY) entity-si — "Stories" cədvəli. Instagram tipli müvəqqəti məzmun.
// Satıcı mağaza adından hekayə paylaşır. Default ömrü = 24 saat (ExpiresAt).
// Admin moderasiya üçün ExpiresAt-ı gözləmədən IsActive-i söndürə bilər. Şəkil + video dəstəklənir.
public class Story : Entity<int>
{
    // Hansı mağazaya aiddir — xarici açar (FK → Store).
    public int StoreId { get; set; }
    // Yüklənmiş şəkil/video faylının URL-i. Məcburidir.
    public required string MediaUrl { get; set; }
    // Məzmun növü: Şəkil yoxsa Video. Default = Image.
    public StoryMediaType MediaType { get; set; } = StoryMediaType.Image;
    // Video üçün üz qabığı (poster) şəkli. Opsionaldır.
    public string? ThumbnailUrl { get; set; }
    // Video uzunluğu (saniyə); şəkillər üçün null. Viewer videonu yarıda kəsməsin və
    // validator (maks. 30 san) yoxlasın deyə lazımdır.
    public int? DurationSeconds { get; set; }
    // Altyazı/mətn. Opsionaldır.
    public string? Caption { get; set; }
    // Hekayədəki keçid ünvanı (URL). Opsionaldır.
    public string? LinkUrl { get; set; }
    // Bitmə vaxtı (adətən yaranma + 24 saat).
    public DateTime ExpiresAt { get; set; }
    // Aktivdirmi? Default = true.
    public bool IsActive { get; set; } = true;
    // Baxış sayı.
    public int ViewCount { get; set; }

    // Aid olduğu mağaza.
    public virtual Store Store { get; set; } = null!;
}
