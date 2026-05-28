using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Chat;

namespace Stylebazar.BLL.Services.Chat;

/// <summary>
/// All chat operations. Every method operates on behalf of the CURRENT user
/// (read from ICurrentUserService) — there is no admin-impersonation surface
/// here. Admin moderation lives behind separate methods (suffix *ForAdmin).
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Returns the current user's inbox — every conversation they participate in,
    /// ordered by most-recently-updated.
    /// </summary>
    Task<Paginate<ConversationDto>> GetMyConversationsAsync(int page = 1, int pageSize = 25, CancellationToken ct = default);

    /// <summary>
    /// Returns the message history for a conversation, newest-first. Caller must
    /// be one of the two participants — throws BusinessException otherwise.
    /// </summary>
    Task<Paginate<MessageDto>> GetMessagesAsync(int conversationId, int page = 1, int pageSize = 30, CancellationToken ct = default);

    /// <summary>
    /// Idempotent: returns the existing conversation between current user and peer,
    /// or creates one if none exists.
    /// </summary>
    Task<ConversationDto> GetOrCreateConversationAsync(string peerUserId, CancellationToken ct = default);

    /// <summary>
    /// Sends a message. Resolves the conversation via dto.RecipientUserId (creating
    /// it if needed). Updates Conversation.LastMessage* + the recipient's Unread*
    /// counter atomically.
    /// </summary>
    Task<MessageDto> SendMessageAsync(SendMessageDto dto, CancellationToken ct = default);

    /// <summary>
    /// Marks all messages in the conversation as read by the current viewer.
    /// Clears their Unread counter. No-op if they aren't a participant.
    /// </summary>
    Task MarkConversationReadAsync(int conversationId, CancellationToken ct = default);

    /// <summary>
    /// Total unread count across all conversations — drives the header badge.
    /// </summary>
    Task<int> GetMyUnreadTotalAsync(CancellationToken ct = default);

    // ----- ADMIN MODERATION (Phase 14.8) -----

    /// <summary>Every conversation in the system, paginated, for the admin overview.</summary>
    Task<Paginate<AdminConversationRowDto>> GetAllConversationsForAdminAsync(int page = 1, int pageSize = 25, CancellationToken ct = default);

    /// <summary>Read-only message history for admin moderation.</summary>
    Task<Paginate<MessageDto>> GetMessagesForAdminAsync(int conversationId, int page = 1, int pageSize = 50, CancellationToken ct = default);

    /// <summary>Soft-deletes a single message for both peers (admin moderation).</summary>
    Task DeleteMessageForAdminAsync(int messageId, CancellationToken ct = default);
}

/// <summary>Admin inbox row — shows BOTH participants since admin isn't one of them.</summary>
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
