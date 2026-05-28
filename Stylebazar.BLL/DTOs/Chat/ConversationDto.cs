namespace Stylebazar.BLL.DTOs.Chat;

/// <summary>
/// Inbox row: who the OTHER participant is + denormalised last-message
/// preview + unread count from the current user's POV.
/// </summary>
public class ConversationDto
{
    public int Id { get; set; }

    /// <summary>The OTHER user (not the current viewer).</summary>
    public string PeerUserId { get; set; } = string.Empty;
    public string PeerFullName { get; set; } = string.Empty;
    public string? PeerAvatarUrl { get; set; }
    public string PeerRole { get; set; } = "Buyer";

    public DateTime LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }

    /// <summary>Unread count from the current viewer's perspective.</summary>
    public int UnreadCount { get; set; }
}
