using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Chat;

namespace Stylebazar.BLL.Services.Chat;

// IChatService — mesajlaşma xidmətinin MÜQAVİLƏSİ (V2). Bütün metodlar CARİ istifadəçi
// adından işləyir (ICurrentUserService); admin moderasiyası *ForAdmin şəkilçili ayrı metodlardadır.
public interface IChatService
{
    // ---- Gələnlər qutusu ----

    // Cari istifadəçinin söhbətləri: sancılanlar öndə, sonra ən son mesaja görə.
    // filter: null/"all" | "unread" | "pinned" | "fav"; search: qarşı tərəfin adına görə axtarış.
    Task<Paginate<ConversationDto>> GetMyConversationsAsync(
        string? filter = null, string? search = null, int page = 1, int pageSize = 25, CancellationToken ct = default);

    // Söhbətin mesaj tarixçəsi (ən yenisi öndə). Çağıran iştirakçı olmalıdır.
    Task<Paginate<MessageDto>> GetMessagesAsync(int conversationId, int page = 1, int pageSize = 30, CancellationToken ct = default);

    // İdempotent: qarşı tərəflə mövcud söhbəti qaytarır, yoxdursa yaradır.
    Task<ConversationDto> GetOrCreateConversationAsync(string peerUserId, CancellationToken ct = default);

    // Sağ panel məlumatı: qarşı tərəfin profili + mağazası + paylaşılmış media + şəxsi parametrlər.
    Task<ConversationDetailDto> GetConversationDetailAsync(int conversationId, CancellationToken ct = default);

    // Söhbət daxilində mətn axtarışı (ən yeni uyğunluqlar).
    Task<List<MessageDto>> SearchMessagesAsync(int conversationId, string query, int take = 20, CancellationToken ct = default);

    // ---- Göndərmə / redaktə / silmə ----

    // Mesaj göndərir (blok yoxlaması ilə). Söhbəti tapır/yaradır, Unread + LastMessage* yenilənir.
    Task<MessageDto> SendMessageAsync(SendMessageDto dto, CancellationToken ct = default);

    // Öz mətn mesajını redaktə edir (EditedAt qoyulur). Yenilənmiş DTO qaytarır.
    Task<MessageDto> EditMessageAsync(int messageId, string newBody, CancellationToken ct = default);

    // Öz mesajını hamı üçün silir (IsDeletedBySender). Yayım üçün (conversationId, peerId) qaytarır.
    Task<(int ConversationId, string PeerId)> DeleteMessageAsync(int messageId, CancellationToken ct = default);

    // Reaksiya qoy/dəyiş/sil (eyni emoji ikinci dəfə → silinir). Yenilənmiş qrupları qaytarır.
    Task<(List<MessageReactionDto> Reactions, int ConversationId, string PeerId)> ReactAsync(
        int messageId, string emoji, CancellationToken ct = default);

    // ---- Oxundu / sayğac ----

    // Söhbəti cari istifadəçi üçün oxundu işarələyir; qarşı tərəfin id-sini qaytarır (yoxsa null).
    Task<string?> MarkConversationReadAsync(int conversationId, CancellationToken ct = default);

    // Bütün söhbətlər üzrə oxunmamış cəm — header rozetkası.
    Task<int> GetMyUnreadTotalAsync(CancellationToken ct = default);

    // ---- Söhbət parametrləri (pin / mute / sevimli / qeyd / etiket) ----

    Task<bool> TogglePinAsync(int conversationId, CancellationToken ct = default);       // yeni vəziyyəti qaytarır
    Task<bool> ToggleMuteAsync(int conversationId, CancellationToken ct = default);
    Task<bool> ToggleFavoriteAsync(int conversationId, CancellationToken ct = default);
    Task SetNoteAsync(int conversationId, string? note, CancellationToken ct = default);
    Task SetTagAsync(int conversationId, string? tag, CancellationToken ct = default);

    // ---- Təhlükəsizlik: blok / şikayət ----

    // Qarşı tərəfi blokla/blokdan çıxar. Yeni vəziyyəti qaytarır (true = indi bloklanıb).
    Task<bool> ToggleBlockAsync(string peerUserId, CancellationToken ct = default);
    // Qarşı tərəfdən şikayət et (admin moderasiyası üçün saxlanır).
    Task ReportUserAsync(string peerUserId, int? conversationId, string reason, CancellationToken ct = default);

    // ---- Sürətli cavablar (satıcı şablonları) ----

    Task<List<QuickReplyDto>> GetMyQuickRepliesAsync(CancellationToken ct = default);
    Task<QuickReplyDto> AddQuickReplyAsync(string title, string body, CancellationToken ct = default);
    Task DeleteQuickReplyAsync(int id, CancellationToken ct = default);

    // ---- ADMIN MODERASİYASI ----

    Task<Paginate<AdminConversationRowDto>> GetAllConversationsForAdminAsync(int page = 1, int pageSize = 25, CancellationToken ct = default);
    Task<Paginate<MessageDto>> GetMessagesForAdminAsync(int conversationId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task DeleteMessageForAdminAsync(int messageId, CancellationToken ct = default);
}

// Admin icmal sətri — admin iştirakçı olmadığı üçün HƏR İKİ tərəfi göstərir.
public class AdminConversationRowDto
{
    public int Id { get; set; }
    public string User1Id { get; set; } = string.Empty;
    public string User1FullName { get; set; } = string.Empty;
    public string User1Role { get; set; } = string.Empty;
    public string User2Id { get; set; } = string.Empty;
    public string User2FullName { get; set; } = string.Empty;
    public string User2Role { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public int MessageCount { get; set; }
}
