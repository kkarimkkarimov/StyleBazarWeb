using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// SÖHBƏT entity-si — "Conversations" cədvəli. İki istifadəçi arasında 1-ə-1 söhbət başlığı.
// Hər cüt üçün YALNIZ BİR sətir: yaradılanda iki id əlifba sırası ilə User1Id/User2Id-yə yığılır
// ki, kimin əvvəl yazmasından asılı olmayaraq təkrar söhbət yaranmasın (unikal indekslə qorunur).
//
// LastMessageAt + LastMessagePreview denormalizasiya olunub ki, gələnlər qutusu siyahısı hər
// sətir üçün ayrıca aqreqat sorğu etməsin (sürət üçün).
public class Conversation : Entity<int>
{
    public string User1Id { get; set; } = string.Empty;   // İki id-dən kiçiyi (əlifba sırası)

    public string User2Id { get; set; } = string.Empty;   // İki id-dən böyüyü

    public DateTime LastMessageAt { get; set; }            // Son mesajın vaxtı (UTC) — qutu sıralamasını idarə edir

    public string? LastMessagePreview { get; set; }        // Son mesajın ~120 simvolluq önizləməsi

    public int Unread1 { get; set; }                       // User1 üçün oxunmamış say (User1 açanda sıfırlanır)

    public int Unread2 { get; set; }                       // User2 üçün oxunmamış say (User2 açanda sıfırlanır)

    public virtual AppUser User1 { get; set; } = null!;                                  // Birinci iştirakçı
    public virtual AppUser User2 { get; set; } = null!;                                  // İkinci iştirakçı
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();    // Söhbətdəki mesajlar
}
