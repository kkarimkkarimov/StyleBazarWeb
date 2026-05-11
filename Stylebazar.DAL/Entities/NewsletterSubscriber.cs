using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

public class NewsletterSubscriber : Entity<int>
{
    public required string Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? UnsubscribedAt { get; set; }
    public required string UnsubscribeToken { get; set; }
}
