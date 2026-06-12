namespace Stylebazar.BLL.DTOs.Chat;

// ConversationDetailDto — messenger-in SAĞ panelinin məlumatı: qarşı tərəfin profili,
// mağazası (satıcıdırsa), paylaşılmış media/fayllar, son paylaşılan məhsul və
// cari istifadəçinin bu söhbətə aid şəxsi parametrləri.
public class ConversationDetailDto
{
    public int ConversationId { get; set; }

    // Qarşı tərəfin profili.
    public string PeerUserId { get; set; } = string.Empty;
    public string PeerFullName { get; set; } = string.Empty;
    public string? PeerAvatarUrl { get; set; }
    public string PeerRole { get; set; } = "Buyer";
    public DateTime? PeerLastSeenAt { get; set; }
    public DateTime PeerMemberSince { get; set; }

    // Qarşı tərəf satıcıdırsa — mağaza kartı.
    public int? StoreId { get; set; }
    public string? StoreName { get; set; }
    public string? StoreSlug { get; set; }
    public string? StoreLogoUrl { get; set; }

    // Bu söhbətdə paylaşılmış şəkillər (son 12) və fayllar (son 10).
    public List<string> SharedImages { get; set; } = new();
    public List<SharedFileDto> SharedFiles { get; set; } = new();
    // Son paylaşılan məhsul (sağ paneldə "Müzakirə olunan məhsul" kartı).
    public ProductPreviewDto? LastProduct { get; set; }

    // Cari istifadəçinin öz tərəfi.
    public bool IsPinned { get; set; }
    public bool IsMuted { get; set; }
    public bool IsFavorite { get; set; }
    public string? Note { get; set; }
    public string? Tag { get; set; }
    // Cari istifadəçi qarşı tərəfi bloklayıb?
    public bool IsBlockedByMe { get; set; }
}

// Paylaşılmış bir faylın qısa təsviri (sağ panel siyahısı üçün).
public class SharedFileDto
{
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? Size { get; set; }
    public DateTime SentAt { get; set; }
}
