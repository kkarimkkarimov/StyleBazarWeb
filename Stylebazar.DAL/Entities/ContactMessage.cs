// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;
// Stylebazar.DAL.Entities.Enums — ContactStatus enum-unu gətirir.
using Stylebazar.DAL.Entities.Enums;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// ƏLAQƏ MESAJI entity-si — "ContactMessages" cədvəli. "Bizimlə əlaqə" formundan gələn mesajlar.
public class ContactMessage : Entity<int>
{
    // Göndərənin ad-soyadı. Məcburidir.
    public required string FullName { get; set; }
    // Göndərənin email-i. Məcburidir.
    public required string Email { get; set; }
    // Mövzu. Opsionaldır.
    public string? Subject { get; set; }
    // Mesajın mətni. Məcburidir.
    public required string Message { get; set; }
    // Vəziyyət: Yeni / oxunmuş / cavablanmış. Default = New.
    public ContactStatus Status { get; set; } = ContactStatus.New;
    // Admin-in daxili qeydi. Opsionaldır.
    public string? AdminNote { get; set; }
    // Göndərənin IP-si (spam/audit üçün). Opsionaldır.
    public string? UserIpAddress { get; set; }
}
