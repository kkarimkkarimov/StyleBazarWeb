using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Chat;

// MessageDto — söhbətdəki bir mesajın UI üçün proyeksiyası. SenderFullName/Avatar kimi
// snapshot sahələr chat panelində N+1 sorğunun qarşısını alır. StoryReply/ProductShare
// üçün daxili önizləmə proyeksiya zamanı doldurulur ki, bubble ikinci sorğusuz çəkilsin.
public class MessageDto
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderFullName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }

    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? Body { get; set; }
    public string? AttachmentUrl { get; set; }
    // Fayl mesajı üçün orijinal ad + ölçü (bayt); səsli mesaj üçün uzunluq (saniyə).
    public string? AttachmentName { get; set; }
    public long? AttachmentSize { get; set; }
    public int? DurationSeconds { get; set; }

    // --- Cavab (reply-to) önizləməsi: hansı mesaja cavabdır ---
    public int? ReplyToMessageId { get; set; }
    public string? ReplyToSenderName { get; set; }
    public string? ReplyToSnippet { get; set; }

    // Redaktə/silinmə vəziyyəti.
    public DateTime? EditedAt { get; set; }
    public bool IsDeletedBySender { get; set; }

    // Mesaja qoyulmuş reaksiyalar (emoji üzrə qruplaşdırılmış).
    public List<MessageReactionDto> Reactions { get; set; } = new();

    // StoryReply mesajları üçün doldurulur.
    public StoryPreviewDto? StoryPreview { get; set; }
    // ProductShare mesajları üçün doldurulur.
    public ProductPreviewDto? ProductPreview { get; set; }

    public bool IsReadByRecipient { get; set; }
    public DateTime CreatedDate { get; set; }
}

// Bir emoji üzrə qruplaşmış reaksiya: emoji + neçə nəfər + cari istifadəçi də qoyub?
public class MessageReactionDto
{
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool Mine { get; set; }
}

// Hekayə cavab bubble-ı üçün minimal hekayə snapshot-u.
public class StoryPreviewDto
{
    public int Id { get; set; }
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public StoryMediaType MediaType { get; set; }
    public string? Caption { get; set; }
    public bool IsExpired { get; set; }
}

// Məhsul paylaşım bubble-ı üçün minimal məhsul snapshot-u.
public class ProductPreviewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
}
