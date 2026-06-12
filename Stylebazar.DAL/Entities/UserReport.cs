// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// İSTİFADƏÇİ ŞİKAYƏTİ entity-si — "UserReports" cədvəli. Mesajlaşmada "Şikayət et"
// düyməsindən gələn qeyd: kim, kimdən, hansı söhbət kontekstində və nə səbəbə şikayət edib.
// Admin moderasiyası üçün saxlanır (IsResolved ilə bağlanır).
public class UserReport : Entity<int>
{
    // Şikayət edən istifadəçi — xarici açar (FK → AppUser). Məcburidir.
    public required string ReporterId { get; set; }
    // Şikayət olunan istifadəçi — xarici açar (FK → AppUser). Məcburidir.
    public required string ReportedUserId { get; set; }
    // Şikayətin aid olduğu söhbət (kontekst üçün). Opsionaldır.
    public int? ConversationId { get; set; }
    // Şikayətin səbəbi (istifadəçinin yazdığı mətn). Məcburidir.
    public required string Reason { get; set; }
    // Admin baxıb həll edib? false = yeni/açıq.
    public bool IsResolved { get; set; }

    // Şikayət edən istifadəçi.
    public virtual AppUser Reporter { get; set; } = null!;
    // Şikayət olunan istifadəçi.
    public virtual AppUser ReportedUser { get; set; } = null!;
    // Əlaqəli söhbət (varsa).
    public virtual Conversation? Conversation { get; set; }
}
