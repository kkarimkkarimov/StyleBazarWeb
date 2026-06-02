using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

// MESAJ entity-si — "Messages" cədvəli. Söhbətin (Conversation) içindəki bir mesaj.
// SenderId — müəllif; o biri iştirakçı söhbət sətrindən bilinir.
//
// Mesajın 4 forması var (MessageType-a bax):
//   - Text:         Body dolu.
//   - Image:        AttachmentUrl dolu (Body-də opsional altyazı).
//   - StoryReply:   Body dolu + StoryId hekayəyə istinad.
//   - ProductShare: ProductId məhsula istinad.
// IsReadByRecipient qarşı tərəf söhbəti açanda true olur (oxundu nişanı).
// IsDeletedForAll admin moderasiyasıdır — true sətirlər hər iki tərəfdən gizlənir, audit üçün qalır.
public class Message : Entity<int>
{
    public int ConversationId { get; set; }                  // Hansı söhbətə aiddir (FK → Conversation)
    public string SenderId { get; set; } = string.Empty;     // Göndərən (FK → AppUser)

    public MessageType MessageType { get; set; } = MessageType.Text;  // Mesaj növü (default: mətn)
    public string? Body { get; set; }                         // Mətn məzmunu
    public string? AttachmentUrl { get; set; }                // Şəkil mesajı üçün fayl URL-i

    public int? StoryId { get; set; }                         // StoryReply üçün — cavab verilən hekayə
    public int? ProductId { get; set; }                       // ProductShare üçün — paylaşılan məhsul

    public bool IsReadByRecipient { get; set; }               // Qarşı tərəf oxuyub?
    public bool IsDeletedForAll { get; set; }                 // Admin tərəfindən silinib? (audit üçün qalır)

    public virtual Conversation Conversation { get; set; } = null!;  // Aid olduğu söhbət
    public virtual AppUser Sender { get; set; } = null!;             // Göndərən istifadəçi
    public virtual Story? Story { get; set; }                        // Əlaqəli hekayə (varsa)
    public virtual Product? Product { get; set; }                    // Əlaqəli məhsul (varsa)
}
