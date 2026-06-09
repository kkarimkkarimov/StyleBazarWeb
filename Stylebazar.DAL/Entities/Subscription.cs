// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;
// Stylebazar.DAL.Entities.Enums — SubscriptionPlan və SubscriptionStatus enum-larını gətirir.
using Stylebazar.DAL.Entities.Enums;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// ABUNƏLİK entity-si — "Subscriptions" cədvəli. İstifadəçinin abunəlik tarixçəsində hər dəyişiklik
// üçün bir audit sətri. Hibrid model: AppUser cari planı saxlayır (tez oxu üçün), bu cədvəl isə
// bütün dəyişikliklərin DƏYİŞMƏZ jurnalıdır (maliyyə/mübahisə üçün).
// Hər istifadəçidə həmişə YALNIZ BİR sətir Status = Active olur. Yüksəltmə köhnə Active sətri
// Expired/Cancelled edib yeni Active sətir əlavə edir.
public class Subscription : Entity<int>
{
    // Kimə aiddir — xarici açar (FK → AppUser). Məcburidir.
    public required string UserId { get; set; }
    // Plan: Free / Pro / VIP.
    public SubscriptionPlan Plan { get; set; }
    // Vəziyyət: Aktiv / Bitib / Ləğv. Default = Active.
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    // Dəyişikliyin tətbiq edildiyi an (UTC).
    public DateTime StartedAt { get; set; }
    // Bitmə tarixi (UTC). null = ömürlük (məs. Free).
    public DateTime? ExpiresAt { get; set; }
    // Dəyişikliyi təsdiqləyən admin-in id-si (audit üçün). Opsionaldır.
    public string? ApprovedByAdminId { get; set; }
    // Sərbəst qeyd ("Kapital Bank, 3-aylıq Pro"). Opsionaldır.
    public string? Notes { get; set; }

    // Abunəlik sahibi olan istifadəçi.
    public virtual AppUser User { get; set; } = null!;
    // Dəyişikliyi təsdiqləyən admin (varsa).
    public virtual AppUser? ApprovedByAdmin { get; set; }
}
