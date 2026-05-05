using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

public class Wishlist : Entity<int>
{
    public required string UserId { get; set; }
    public int ProductId { get; set; }

    public virtual AppUser User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
