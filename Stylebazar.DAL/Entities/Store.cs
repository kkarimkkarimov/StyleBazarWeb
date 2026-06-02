using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// MAĞAZA entity-si — "Stores" cədvəli. Hər satıcının öz mağazası var; məhsullar
// mağazaya bağlanır. Marketplace-in əsas obyektlərindən biridir.
public class Store : Entity<int>
{
    public required string Name { get; set; }              // Mağaza adı (məcburi)
    public required string Slug { get; set; }              // URL üçün təmiz ad
    public string? Description { get; set; }                // Mağaza haqqında
    public required string ProfileImageUrl { get; set; }    // Profil/logo şəkli (məcburi)
    public string? CoverImageUrl { get; set; }              // Üst (cover) banner şəkli
    public string? ContactPhone { get; set; }               // Əlaqə telefonu
    public string? Address { get; set; }                    // Ünvan
    public required string OwnerId { get; set; }            // Sahibinin id-si (FK → AppUser)
    public bool IsActive { get; set; }                      // Aktivdir? (false = gizli)

    // --- Əlaqə kanalları (marketplace UX-in ürəyi — alıcı buradan satıcıya yazır) ---

    // WhatsApp nömrəsi yalnız rəqəmlərlə saxlanılır (məs. "994508204111").
    // Klik olunanda UI əvvəlinə https://wa.me/ əlavə edir.
    public string? WhatsAppNumber { get; set; }

    // Instagram istifadəçi adı (@ olmadan) → instagram.com/istifadeci
    public string? InstagramHandle { get; set; }

    // Telegram istifadəçi adı (@ olmadan) → t.me/istifadeci
    public string? TelegramHandle { get; set; }

    // Facebook səhifə adı → facebook.com/sehife
    public string? FacebookHandle { get; set; }

    // TikTok istifadəçi adı (@ olmadan) → tiktok.com/@istifadeci
    public string? TikTokHandle { get; set; }

    // --- Naviqasiya sahələri ---
    public virtual AppUser Owner { get; set; } = null!;                                       // Mağaza sahibi (istifadəçi)
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();          // Mağazanın məhsulları
    public virtual ICollection<StoreFollower> Followers { get; set; } = new List<StoreFollower>(); // Mağazanı izləyənlər
}
