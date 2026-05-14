using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// One active cart per user. We keep it as a long-lived row (no expiry) so the
/// user can come back days later and still see their basket. Checkout converts
/// the cart's items into an Order and clears (or archives) the cart.
/// </summary>
public class Cart : Entity<int>
{
    public required string UserId { get; set; }

    public virtual AppUser User { get; set; } = null!;
    public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
