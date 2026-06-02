using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// SEÇİLMİŞLƏR entity-si — "Wishlists" cədvəli. İstifadəçi ilə bəyəndiyi məhsulu birləşdirən
// əlaqə cədvəli (junction table). Hər sətir = bir istifadəçi bir məhsulu seçilmişə əlavə edib.
public class Wishlist : Entity<int>
{
    public required string UserId { get; set; }   // Kim seçib (FK → AppUser)
    public int ProductId { get; set; }            // Hansı məhsulu (FK → Product)

    public virtual AppUser User { get; set; } = null!;     // Seçimi edən istifadəçi
    public virtual Product Product { get; set; } = null!;  // Seçilən məhsul
}
