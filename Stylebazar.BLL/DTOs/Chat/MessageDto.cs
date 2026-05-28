using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Chat;

/// <summary>
/// One message in a conversation, projected for the UI. Snapshot fields like
/// SenderFullName / SenderAvatarUrl avoid an N+1 in the chat panel.
///
/// For StoryReply / ProductShare types, the embedded preview is populated
/// at projection time so the bubble can render without a second roundtrip.
/// </summary>
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

    /// <summary>Filled for StoryReply messages.</summary>
    public StoryPreviewDto? StoryPreview { get; set; }

    /// <summary>Filled for ProductShare messages.</summary>
    public ProductPreviewDto? ProductPreview { get; set; }

    public bool IsReadByRecipient { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>Minimal story snapshot for inline rendering in a StoryReply bubble.</summary>
public class StoryPreviewDto
{
    public int Id { get; set; }
    public string? MediaUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public StoryMediaType MediaType { get; set; }
    public string? Caption { get; set; }
    public bool IsExpired { get; set; }
}

/// <summary>Minimal product snapshot for inline rendering in a ProductShare bubble.</summary>
public class ProductPreviewDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
}
