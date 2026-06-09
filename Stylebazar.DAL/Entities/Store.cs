// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// MAĞAZA entity-si — "Stores" cədvəli. Hər satıcının öz mağazası var; məhsullar mağazaya bağlanır.
// Marketplace-in əsas obyektlərindən biridir. Entity<int>-dən Id + audit tarixləri gəlir.
public class Store : Entity<int>
{
    // Mağaza adı. Məcburidir.
    public required string Name { get; set; }
    // URL üçün təmiz ad (slug). Məcburidir.
    public required string Slug { get; set; }
    // Mağaza haqqında təsvir. Opsionaldır.
    public string? Description { get; set; }
    // Profil/logo şəkli. Məcburidir.
    public required string ProfileImageUrl { get; set; }
    // Üst (cover) banner şəkli. Opsionaldır.
    public string? CoverImageUrl { get; set; }
    // Əlaqə telefonu. Opsionaldır.
    public string? ContactPhone { get; set; }
    // Mağazanın fiziki ünvanı. Opsionaldır.
    public string? Address { get; set; }
    // Sahibinin id-si — xarici açar (FK → AppUser). Məcburidir.
    public required string OwnerId { get; set; }
    // Aktivdirmi? false → mağaza gizlədilir.
    public bool IsActive { get; set; }

    // --- Əlaqə kanalları (marketplace UX-in ürəyi — alıcı buradan satıcıya yazır) ---

    // WhatsApp nömrəsi yalnız rəqəmlərlə saxlanır (məs. "994508204111").
    // Klik olunanda UI əvvəlinə https://wa.me/ əlavə edir.
    public string? WhatsAppNumber { get; set; }
    // Instagram istifadəçi adı (@ olmadan) → instagram.com/istifadeci.
    public string? InstagramHandle { get; set; }
    // Telegram istifadəçi adı (@ olmadan) → t.me/istifadeci.
    public string? TelegramHandle { get; set; }
    // Facebook səhifə adı → facebook.com/sehife.
    public string? FacebookHandle { get; set; }
    // TikTok istifadəçi adı (@ olmadan) → tiktok.com/@istifadeci.
    public string? TikTokHandle { get; set; }

    // --- Naviqasiya sahələri ---
    // Mağaza sahibi olan istifadəçi (çox mağaza → bir sahib).
    public virtual AppUser Owner { get; set; } = null!;
    // Mağazanın məhsulları (bir mağaza → çox məhsul).
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    // Mağazanı izləyən istifadəçilərin qeydləri.
    public virtual ICollection<StoreFollower> Followers { get; set; } = new List<StoreFollower>();
}
