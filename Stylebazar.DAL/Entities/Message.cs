// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;
// Stylebazar.DAL.Entities.Enums — MessageType enum-unu gətirir.
using Stylebazar.DAL.Entities.Enums;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// MESAJ entity-si — "Messages" cədvəli. Söhbətin (Conversation) içindəki bir mesaj.
// SenderId — müəllif; o biri iştirakçı söhbət sətrindən bilinir.
// Mesajın 4 forması var (MessageType-a bax):
//   - Text:         Body dolu.
//   - Image:        AttachmentUrl dolu (Body-də opsional altyazı).
//   - StoryReply:   Body dolu + StoryId hekayəyə istinad.
//   - ProductShare: ProductId məhsula istinad.
// IsReadByRecipient qarşı tərəf söhbəti açanda true olur (oxundu nişanı).
// IsDeletedForAll admin moderasiyasıdır — true sətirlər hər iki tərəfdən gizlənir, audit üçün qalır.
public class Message : Entity<int>
{
    // Hansı söhbətə aiddir — xarici açar (FK → Conversation).
    public int ConversationId { get; set; }
    // Göndərən — xarici açar (FK → AppUser). Default = boş sətir.
    public string SenderId { get; set; } = string.Empty;
    // Mesaj növü (mətn / şəkil / hekayə-cavabı / məhsul-paylaşımı). Default = Text.
    public MessageType MessageType { get; set; } = MessageType.Text;
    // Mətn məzmunu. Opsionaldır (şəkil mesajında null ola bilər).
    public string? Body { get; set; }
    // Şəkil mesajı üçün fayl URL-i. Opsionaldır.
    public string? AttachmentUrl { get; set; }
    // StoryReply üçün — cavab verilən hekayənin id-si. Opsionaldır.
    public int? StoryId { get; set; }
    // ProductShare üçün — paylaşılan məhsulun id-si. Opsionaldır.
    public int? ProductId { get; set; }
    // Qarşı tərəf bu mesajı oxuyub?
    public bool IsReadByRecipient { get; set; }
    // Admin tərəfindən silinib? (sətir audit üçün qalır, sadəcə gizlədilir).
    public bool IsDeletedForAll { get; set; }

    // Aid olduğu söhbət.
    public virtual Conversation Conversation { get; set; } = null!;
    // Göndərən istifadəçi.
    public virtual AppUser Sender { get; set; } = null!;
    // Əlaqəli hekayə (StoryReply olduqda).
    public virtual Story? Story { get; set; }
    // Əlaqəli məhsul (ProductShare olduqda).
    public virtual Product? Product { get; set; }
}
