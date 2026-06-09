// Enum-ların yerləşdiyi ad fəzası.
namespace Stylebazar.DAL.Entities.Enums;

// ABUNƏLİK SƏTRİNİN VƏZİYYƏTİ enum-u. İstifadəçidə istənilən anda ən çoxu BİR sətir Active olur.
public enum SubscriptionStatus
{
    Active = 0,     // Hazırda etibarlı (StartedAt ilə ExpiresAt arasında).
    Expired = 1,    // ExpiresAt keçib (növbəti /Seller/* girişində avtomatik qoyulur).
    Cancelled = 2   // Admin və ya istifadəçi vaxtından əvvəl dayandırıb.
}
