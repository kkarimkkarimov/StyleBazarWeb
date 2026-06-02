using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// RƏY entity-si — "Reviews" cədvəli. İstifadəçinin məhsula yazdığı qiymətləndirmə (ulduz + şərh).
public class Review : Entity<int>
{
    public required string UserId { get; set; }   // Rəyi yazan istifadəçi (FK → AppUser)
    public int ProductId { get; set; }            // Hansı məhsula yazılıb (FK → Product)
    public int Rating { get; set; }               // Ulduz sayı (1–5)
    public string? Comment { get; set; }          // Mətnli şərh (opsional)

    // --- Naviqasiya sahələri ---
    public virtual AppUser User { get; set; } = null!;                                       // Rəyin müəllifi
    public virtual Product Product { get; set; } = null!;                                    // Rəy yazılan məhsul
    public virtual ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();   // Rəyə əlavə olunan şəkillər
}
