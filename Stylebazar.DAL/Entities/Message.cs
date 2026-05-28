using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// A single message inside a <see cref="Conversation"/>. SenderId is the
/// author; the other participant is inferred from the conversation row.
///
/// Message has 4 shapes (see <see cref="Enums.MessageType"/>):
///   - Text:         Body filled.
///   - Image:        AttachmentUrl filled (caption optional in Body).
///   - StoryReply:   Body filled + StoryId references the story.
///   - ProductShare: Body optional + ProductId references the product.
/// IsReadByRecipient flips when the OTHER user opens the thread (mark-as-read).
/// IsDeletedForAll is admin moderation — true rows are hidden from both peers
/// but kept for audit.
/// </summary>
public class Message : Entity<int>
{
    public int ConversationId { get; set; }
    public string SenderId { get; set; } = string.Empty;

    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? Body { get; set; }
    public string? AttachmentUrl { get; set; }

    /// <summary>For StoryReply messages — the story being replied to.</summary>
    public int? StoryId { get; set; }

    /// <summary>For ProductShare messages — the product being shared.</summary>
    public int? ProductId { get; set; }

    /// <summary>True once the recipient opens the thread.</summary>
    public bool IsReadByRecipient { get; set; }

    /// <summary>Admin moderation flag — hides the row from both peers but keeps it for audit.</summary>
    public bool IsDeletedForAll { get; set; }

    public virtual Conversation Conversation { get; set; } = null!;
    public virtual AppUser Sender { get; set; } = null!;
    public virtual Story? Story { get; set; }
    public virtual Product? Product { get; set; }
}
