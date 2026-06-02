using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// XƏBƏR BÜLLETENİ ABUNƏÇİSİ entity-si — "NewsletterSubscribers" cədvəli.
// Email ilə abunə olanlar. Abunəlikdən çıxış unikal token ilə təhlükəsiz edilir.
public class NewsletterSubscriber : Entity<int>
{
    public required string Email { get; set; }              // Abunəçinin email-i
    public bool IsActive { get; set; } = true;              // Hələ abunədir? (default: bəli)
    public DateTime? UnsubscribedAt { get; set; }           // Abunəlikdən çıxdığı tarix (null = çıxmayıb)
    public required string UnsubscribeToken { get; set; }    // Çıxış üçün unikal token (linkdə işlənir)
}
