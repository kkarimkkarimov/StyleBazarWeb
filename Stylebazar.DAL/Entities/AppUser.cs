using Microsoft.AspNetCore.Identity;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities
{
    // İSTİFADƏÇİ entity-si — "AspNetUsers" cədvəli.
    // IdentityUser<string>-dən miras alır → ASP.NET Core Identity-nin hazır
    // sahələrini (Email, PasswordHash, UserName və s.) avtomatik gətirir.
    // Biz ona yalnız öz əlavə sahələrimizi əlavə edirik.
    public class AppUser : IdentityUser<string>
    {
        public required string FullName { get; set; }   // Ad-soyad (məcburi)
        public string? ProfileImageUrl { get; set; }    // Profil şəkli (varsa)
        public string? Bio { get; set; }                // Qısa haqqında məlumat
        public DateTime CreatedDate { get; set; }       // Qeydiyyat tarixi

        // ------ Denormalizasiya olunmuş abunəlik vəziyyəti (hibrid model) ------
        // Subscription cədvəli bütün dəyişiklik tarixçəsini saxlayır; bu iki sahə
        // isə *cari* planı burada da saxlayır ki, tez-tez oxunan yerlərdə (hər
        // /Seller/* sorğusu, kataloq sıralaması) əlavə JOIN olmadan sürətli oxunsun.

        // Aktiv plan səviyyəsi. Hər yeni istifadəçi üçün default = Free (pulsuz).
        public SubscriptionPlan CurrentPlan { get; set; } = SubscriptionPlan.Free;

        // Planın bitmə tarixi (UTC). null = bitmə yoxdur (ömürlük / Free).
        // UtcNow-dan kiçik olanda → avtomatik Free-yə endirmə işə düşür.
        public DateTime? PlanExpiresAt { get; set; }

        // --- Naviqasiya sahələri (istifadəçinin sahib olduğu məlumatlar) ---
        public virtual ICollection<Store> Stores { get; set; } = new List<Store>();                 // İstifadəçinin mağazaları
        public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();        // Seçilmişləri
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();              // Yazdığı rəylər
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();          // Ünvanları
        public virtual ICollection<StoreFollower> StoreFollows { get; set; } = new List<StoreFollower>(); // İzlədiyi mağazalar
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();  // Abunəlik tarixçəsi
    }
}
