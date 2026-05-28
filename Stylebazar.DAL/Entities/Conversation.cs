using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// 1-to-1 thread between two users. Stored ONCE per pair — we sort the two
/// user-ids alphabetically into User1Id / User2Id at create time so the same
/// pair never gets two conversations regardless of who started chatting first.
/// (Enforced by a unique index on (User1Id, User2Id).)
///
/// LastMessageAt + LastMessagePreview are denormalised so the inbox list
/// doesn't need a per-row aggregate query.
/// </summary>
public class Conversation : Entity<int>
{
    /// <summary>Lower of the two user-ids (alphabetical sort).</summary>
    public string User1Id { get; set; } = string.Empty;

    /// <summary>Higher of the two user-ids.</summary>
    public string User2Id { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the latest message — drives inbox ordering.</summary>
    public DateTime LastMessageAt { get; set; }

    /// <summary>~120 chars; the inbox list shows this under the participant name.</summary>
    public string? LastMessagePreview { get; set; }

    /// <summary>Total unread for User1 (cleared when User1 opens the thread).</summary>
    public int Unread1 { get; set; }

    /// <summary>Total unread for User2 (cleared when User2 opens the thread).</summary>
    public int Unread2 { get; set; }

    public virtual AppUser User1 { get; set; } = null!;
    public virtual AppUser User2 { get; set; } = null!;
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
