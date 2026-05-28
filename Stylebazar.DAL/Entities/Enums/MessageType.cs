namespace Stylebazar.DAL.Entities.Enums;

/// <summary>
/// What a Message carries. Each row picks exactly one:
///   - Text:         free-text body (default)
///   - Image:        AttachmentUrl points to an uploaded image
///   - StoryReply:   text body + StoryId references the story being replied to;
///                   recipient sees a small story preview above the bubble
///   - ProductShare: text body + ProductId references a product card
/// </summary>
public enum MessageType
{
    Text = 0,
    Image = 1,
    StoryReply = 2,
    ProductShare = 3
}
