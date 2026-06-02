using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

// ƏLAQƏ MESAJI entity-si — "ContactMessages" cədvəli. "Bizimlə əlaqə" formundan gələn mesajlar.
public class ContactMessage : Entity<int>
{
    public required string FullName { get; set; }   // Göndərənin ad-soyadı
    public required string Email { get; set; }       // Göndərənin email-i
    public string? Subject { get; set; }             // Mövzu (opsional)
    public required string Message { get; set; }     // Mesajın mətni
    public ContactStatus Status { get; set; } = ContactStatus.New;  // Vəziyyət: Yeni / oxunmuş / cavablanmış (default: Yeni)
    public string? AdminNote { get; set; }           // Admin-in daxili qeydi
    public string? UserIpAddress { get; set; }       // Göndərənin IP-si (spam/audit üçün)
}
