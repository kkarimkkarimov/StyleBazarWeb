using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Chat;

/// <summary>
/// Inbound from REST POST and SignalR Hub.SendMessage. Conversation is
/// resolved via RecipientUserId — first call between two users creates it.
/// AttachmentUrl is set by the controller AFTER it saves the IFormFile;
/// the DTO itself carries only metadata.
/// </summary>
public class SendMessageDto
{
    public string RecipientUserId { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? Body { get; set; }
    public string? AttachmentUrl { get; set; }
    public int? StoryId { get; set; }
    public int? ProductId { get; set; }
}
