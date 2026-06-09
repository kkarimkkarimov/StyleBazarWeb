// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// RƏY entity-si — "Reviews" cədvəli. İstifadəçinin məhsula yazdığı qiymətləndirmə (ulduz + şərh).
public class Review : Entity<int>
{
    // Rəyi yazan istifadəçinin id-si — xarici açar (FK → AppUser). Məcburidir.
    public required string UserId { get; set; }
    // Hansı məhsula yazılıb — xarici açar (FK → Product).
    public int ProductId { get; set; }
    // Ulduz sayı (1–5 aralığında).
    public int Rating { get; set; }
    // Mətnli şərh. Opsionaldır (yalnız ulduz da vermək olar).
    public string? Comment { get; set; }

    // --- Naviqasiya sahələri ---
    // Rəyin müəllifi olan istifadəçi.
    public virtual AppUser User { get; set; } = null!;
    // Rəy yazılmış məhsul.
    public virtual Product Product { get; set; } = null!;
    // Rəyə əlavə olunmuş şəkillər (foto-rəy).
    public virtual ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
}
