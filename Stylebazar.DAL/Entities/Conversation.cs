// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// SÖHBƏT entity-si — "Conversations" cədvəli. İki istifadəçi arasında 1-ə-1 söhbət başlığı.
// Hər cüt üçün YALNIZ BİR sətir: yaradılanda iki id əlifba sırası ilə User1Id/User2Id-yə yığılır
// ki, kimin əvvəl yazmasından asılı olmayaraq təkrar söhbət yaranmasın (unikal indekslə qorunur).
// LastMessageAt + LastMessagePreview denormalizasiya olunub ki, gələnlər qutusu siyahısı hər sətir
// üçün ayrıca aqreqat sorğu etməsin (sürət üçün).
public class Conversation : Entity<int>
{
    // İki id-dən KİÇİYİ (əlifba sırası ilə). Default = boş sətir.
    public string User1Id { get; set; } = string.Empty;
    // İki id-dən BÖYÜYÜ. Default = boş sətir.
    public string User2Id { get; set; } = string.Empty;
    // Son mesajın vaxtı (UTC) — gələnlər qutusunun sıralamasını idarə edir.
    public DateTime LastMessageAt { get; set; }
    // Son mesajın ~120 simvolluq önizləməsi. Opsionaldır.
    public string? LastMessagePreview { get; set; }
    // User1 üçün oxunmamış mesaj sayı (User1 söhbəti açanda sıfırlanır).
    public int Unread1 { get; set; }
    // User2 üçün oxunmamış mesaj sayı (User2 söhbəti açanda sıfırlanır).
    public int Unread2 { get; set; }

    // --- Messaging V2: hər iştirakçının ÖZ tərəfinə aid parametrlər (1 = User1, 2 = User2) ---
    // Söhbət sancılıb? (pinned — siyahının yuxarısında durur).
    public bool Pinned1 { get; set; }
    public bool Pinned2 { get; set; }
    // Söhbət səssizə alınıb? (muted — bildiriş səsi/brauzer bildirişi gəlmir).
    public bool Muted1 { get; set; }
    public bool Muted2 { get; set; }
    // Sevimli kimi işarələnib? (satıcının "sevimli müştərilər" filtri üçün).
    public bool Fav1 { get; set; }
    public bool Fav2 { get; set; }
    // İştirakçının qarşı tərəf haqqında ŞƏXSİ qeydi (yalnız özü görür; satıcı aləti).
    public string? Note1 { get; set; }
    public string? Note2 { get; set; }
    // İştirakçının bu söhbətə verdiyi etiket (məs. "VIP", "Yeni", "Sifariş").
    public string? Tag1 { get; set; }
    public string? Tag2 { get; set; }

    // Birinci iştirakçı.
    public virtual AppUser User1 { get; set; } = null!;
    // İkinci iştirakçı.
    public virtual AppUser User2 { get; set; } = null!;
    // Söhbətdəki mesajlar (bir söhbət → çox mesaj).
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
