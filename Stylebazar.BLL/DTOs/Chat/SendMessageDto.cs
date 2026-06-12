using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Chat;

// SendMessageDto — REST POST və SignalR Hub.SendMessage-dən gələn giriş. Söhbət
// RecipientUserId ilə tapılır (ilk yazışmada yaradılır). AttachmentUrl-i controller
// faylı yadda saxladıqdan SONRA doldurur; DTO özü yalnız metadata daşıyır.
public class SendMessageDto
{
    public string RecipientUserId { get; set; } = string.Empty;
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? Body { get; set; }
    public string? AttachmentUrl { get; set; }
    public int? StoryId { get; set; }
    public int? ProductId { get; set; }

    // --- V2 sahələri ---
    // Cavab verilən mesajın id-si (reply-to). Opsionaldır.
    public int? ReplyToMessageId { get; set; }
    // Fayl mesajı üçün orijinal ad + ölçü (controller doldurur).
    public string? AttachmentName { get; set; }
    public long? AttachmentSize { get; set; }
    // Səsli mesajın uzunluğu (saniyə; client ölçür, server 1–300 aralığında yoxlayır).
    public int? DurationSeconds { get; set; }
}
