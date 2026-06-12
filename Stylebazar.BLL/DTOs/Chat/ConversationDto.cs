namespace Stylebazar.BLL.DTOs.Chat;

// ConversationDto — gələnlər qutusunun BİR sətri: qarşı tərəf kim + son mesaj önizləməsi +
// cari istifadəçinin nöqteyi-nəzərindən oxunmamış sayı və şəxsi parametrlər (pin/mute/sevimli).
public class ConversationDto
{
    public int Id { get; set; }

    // Qarşı tərəf (cari baxan YOX, o biri istifadəçi).
    public string PeerUserId { get; set; } = string.Empty;
    public string PeerFullName { get; set; } = string.Empty;
    public string? PeerAvatarUrl { get; set; }
    public string PeerRole { get; set; } = "Buyer";
    // Qarşı tərəfin son görülmə vaxtı (UTC). null = məlum deyil.
    public DateTime? PeerLastSeenAt { get; set; }
    // Qarşı tərəf satıcıdırsa — mağazasının adı/slug-u (siyahıda kontekst + sağ panel üçün).
    public string? PeerStoreName { get; set; }
    public string? PeerStoreSlug { get; set; }

    public DateTime LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }

    // Cari baxanın nöqteyi-nəzərindən oxunmamış mesaj sayı.
    public int UnreadCount { get; set; }

    // --- Cari baxanın ÖZ tərəfinə aid parametrlər ---
    public bool IsPinned { get; set; }     // sancılıb (siyahının yuxarısında)
    public bool IsMuted { get; set; }      // səssizə alınıb (bildiriş yox)
    public bool IsFavorite { get; set; }   // sevimli (satıcı filtri)
    public string? Note { get; set; }      // şəxsi qeyd (yalnız özü görür)
    public string? Tag { get; set; }       // etiket (məs. "VIP")
}
