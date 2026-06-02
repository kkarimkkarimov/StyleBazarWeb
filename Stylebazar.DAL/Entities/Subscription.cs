using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

// ABUNƏLİK entity-si — "Subscriptions" cədvəli. İstifadəçinin abunəlik tarixçəsində hər
// dəyişiklik üçün bir audit sətri. Hibrid model: AppUser cari planı saxlayır (tez oxu üçün),
// bu cədvəl isə bütün dəyişikliklərin DƏYİŞMƏZ jurnalıdır (maliyyə/mübahisə üçün).
//
// Hər istifadəçidə həmişə YALNIZ BİR sətir Status = Active olur. Yüksəltmə (upgrade) köhnə
// Active sətri Expired/Cancelled edib yeni Active sətir əlavə edir.
public class Subscription : Entity<int>
{
    public required string UserId { get; set; }    // Kimə aiddir (FK → AppUser)

    public SubscriptionPlan Plan { get; set; }     // Plan: Free / Pro / VIP
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;  // Vəziyyət (default: Aktiv)

    public DateTime StartedAt { get; set; }        // Dəyişikliyin tətbiq edildiyi an (UTC)

    public DateTime? ExpiresAt { get; set; }       // Bitmə tarixi (UTC); null = ömürlük (məs. Free)

    public string? ApprovedByAdminId { get; set; } // Dəyişikliyi təsdiqləyən admin-in id-si (audit üçün)

    public string? Notes { get; set; }             // Sərbəst qeyd ("Kapital Bank, 3-aylıq Pro")

    public virtual AppUser User { get; set; } = null!;       // Abunəlik sahibi
    public virtual AppUser? ApprovedByAdmin { get; set; }    // Təsdiqləyən admin
}
