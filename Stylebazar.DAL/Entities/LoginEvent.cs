// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;
// Stylebazar.DAL.Entities.Enums — LoginEventType enum-unu gətirir.
using Stylebazar.DAL.Entities.Enums;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// GİRİŞ HADİSƏSİ entity-si — "LoginEvents" cədvəli. Hər autentifikasiya hadisəsi (giriş, uğursuz
// cəhd və s.) üçün audit sətri. Profil → Təhlükəsizlik → Giriş tarixçəsində göstərilir.
public class LoginEvent : Entity<int>
{
    // Kimə aiddir — xarici açar (FK → AppUser). Məcburidir.
    public required string UserId { get; set; }
    // Hadisə növü (giriş / çıxış / uğursuz cəhd...).
    public LoginEventType EventType { get; set; }
    // Mənbə IP (proxy arxasında X-Forwarded-For nəzərə alınır). Opsionaldır.
    public string? IpAddress { get; set; }
    // Brauzer/cihaz məlumatı (User-Agent başlığı). Opsionaldır.
    public string? UserAgent { get; set; }

    // Aid olduğu istifadəçi.
    public virtual AppUser User { get; set; } = null!;
}
