using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

// GİRİŞ HADİSƏSİ entity-si — "LoginEvents" cədvəli. Hər autentifikasiya hadisəsi (giriş,
// uğursuz cəhd və s.) üçün audit sətri. Profil → Təhlükəsizlik → Giriş tarixçəsində göstərilir.
public class LoginEvent : Entity<int>
{
    public required string UserId { get; set; }       // Kimə aiddir (FK → AppUser)
    public LoginEventType EventType { get; set; }     // Hadisə növü (giriş / çıxış / uğursuz cəhd...)

    public string? IpAddress { get; set; }            // Mənbə IP (proxy arxasında X-Forwarded-For nəzərə alınır)

    public string? UserAgent { get; set; }            // Brauzer/cihaz məlumatı (User-Agent başlığı)

    public virtual AppUser User { get; set; } = null!;  // Aid olduğu istifadəçi
}
