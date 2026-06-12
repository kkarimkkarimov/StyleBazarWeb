// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// MESAJ REAKSİYASI entity-si — "MessageReactions" cədvəli. Bir istifadəçinin bir mesaja
// qoyduğu emoji reaksiyası (WhatsApp/Telegram üslubunda). (MessageId, UserId) UNİKALDIR —
// hər istifadəçi bir mesaja yalnız BİR reaksiya qoya bilər; yenisini seçəndə köhnəsi əvəzlənir.
public class MessageReaction : Entity<int>
{
    // Hansı mesaja qoyulub — xarici açar (FK → Message).
    public int MessageId { get; set; }
    // Kim qoyub — xarici açar (FK → AppUser). Məcburidir.
    public required string UserId { get; set; }
    // Emoji simvolu (məs. "❤️", "👍", "😂"). Qısa saxlanır.
    public required string Emoji { get; set; }

    // Reaksiya qoyulan mesaj.
    public virtual Message Message { get; set; } = null!;
    // Reaksiyanı qoyan istifadəçi.
    public virtual AppUser User { get; set; } = null!;
}
