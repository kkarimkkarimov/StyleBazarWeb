// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// SEÇİLMİŞLƏR entity-si — "Wishlists" cədvəli. İstifadəçi ilə bəyəndiyi məhsulu birləşdirən
// əlaqə (junction) cədvəli. Hər sətir = bir istifadəçi bir məhsulu seçilmişə əlavə edib.
public class Wishlist : Entity<int>
{
    // Kim seçib — xarici açar (FK → AppUser). Məcburidir.
    public required string UserId { get; set; }
    // Hansı məhsulu seçib — xarici açar (FK → Product).
    public int ProductId { get; set; }

    // Seçimi edən istifadəçi.
    public virtual AppUser User { get; set; } = null!;
    // Seçilən məhsul.
    public virtual Product Product { get; set; } = null!;
}
