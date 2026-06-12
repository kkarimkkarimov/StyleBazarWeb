// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// SÜRƏTLİ CAVAB entity-si — "QuickReplies" cədvəli. Satıcının əvvəlcədən hazırladığı cavab
// şablonları (məs. "Salam! Hansı ölçü lazımdır?"). Composer-də bir kliklə daxil edilir.
public class QuickReply : Entity<int>
{
    // Şablonun sahibi — xarici açar (FK → AppUser). Məcburidir.
    public required string UserId { get; set; }
    // Qısa başlıq (menyuda görünür, məs. "Salamlama"). Məcburidir.
    public required string Title { get; set; }
    // Şablonun tam mətni (composer-ə daxil edilir). Məcburidir.
    public required string Body { get; set; }

    // Şablonun sahibi olan istifadəçi.
    public virtual AppUser User { get; set; } = null!;
}
