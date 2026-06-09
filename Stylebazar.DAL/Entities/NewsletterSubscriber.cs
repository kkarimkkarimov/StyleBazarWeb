// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// XƏBƏR BÜLLETENİ ABUNƏÇİSİ entity-si — "NewsletterSubscribers" cədvəli.
// Email ilə abunə olanlar. Abunəlikdən çıxış unikal token ilə təhlükəsiz aparılır.
public class NewsletterSubscriber : Entity<int>
{
    // Abunəçinin email-i. Məcburidir.
    public required string Email { get; set; }
    // Hələ abunədirmi? Default = true.
    public bool IsActive { get; set; } = true;
    // Abunəlikdən çıxdığı tarix. null = çıxmayıb.
    public DateTime? UnsubscribedAt { get; set; }
    // Çıxış üçün unikal token (abunəlikdən-çıx linkində işlənir). Məcburidir.
    public required string UnsubscribeToken { get; set; }
}
