// Microsoft.AspNetCore.Identity — IdentityUser<string> baza sinfini gətirir (hazır istifadəçi sistemi).
using Microsoft.AspNetCore.Identity;
// Stylebazar.DAL.Entities.Enums — SubscriptionPlan enum-unu gətirir.
using Stylebazar.DAL.Entities.Enums;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities
{
    // İSTİFADƏÇİ entity-si — "AspNetUsers" cədvəli.
    // IdentityUser<string>-dən miras alır → Email, PasswordHash, UserName, PhoneNumber və s.
    // sahələri AVTOMATİK gəlir. Biz yalnız öz əlavə sahələrimizi qoşuruq.
    public class AppUser : IdentityUser<string>
    {
        // Ad-soyad. "required" → mütləq doldurulmalıdır.
        public required string FullName { get; set; }
        // Profil şəkli URL-i. Opsionaldır (null ola bilər).
        public string? ProfileImageUrl { get; set; }
        // Qısa "haqqında" mətni. Opsionaldır.
        public string? Bio { get; set; }
        // Qeydiyyat tarixi.
        public DateTime CreatedDate { get; set; }

        // ------ Denormalizasiya olunmuş abunəlik vəziyyəti (hibrid model) ------
        // Subscription cədvəli bütün dəyişiklik TARİXÇƏSİNİ saxlayır; bu iki sahə isə
        // CARİ planı burada da saxlayır ki, tez-tez oxunan yerlər (hər /Seller/* sorğusu,
        // kataloq sıralaması) əlavə JOIN olmadan sürətli işləsin.

        // Aktiv plan səviyyəsi. Hər yeni istifadəçidə default = Free (pulsuz).
        public SubscriptionPlan CurrentPlan { get; set; } = SubscriptionPlan.Free;

        // Planın bitmə tarixi (UTC). null = bitmə yoxdur (Free / ömürlük).
        // UtcNow-dan kiçik olanda oxu zamanı avtomatik Free-yə endirmə işə düşür.
        public DateTime? PlanExpiresAt { get; set; }

        // Mesajlaşmada "son görülmə" (UTC) — istifadəçi oflayn olanda chat hub yazır.
        // null = heç vaxt qoşulmayıb (və ya hələ yazılmayıb).
        public DateTime? LastSeenAt { get; set; }

        // --- Naviqasiya sahələri (istifadəçinin sahib olduğu məlumatlar) ---
        // İstifadəçinin mağazaları (bir istifadəçi → çox mağaza).
        public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
        // Seçilmişlər (wishlist) qeydləri.
        public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        // İstifadəçinin yazdığı rəylər.
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        // İstifadəçinin çatdırılma ünvanları.
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        // İzlədiyi mağazalar.
        public virtual ICollection<StoreFollower> StoreFollows { get; set; } = new List<StoreFollower>();
        // Abunəlik dəyişiklik tarixçəsi.
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    }
}
