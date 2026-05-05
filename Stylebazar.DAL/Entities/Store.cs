using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

public class Store : Entity<int>
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Description { get; set; }
    public required string ProfileImageUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public required string ContactPhone { get; set; }
    public required string Address { get; set; }
    public required string OwnerId { get; set; }
    public bool IsActive { get; set; }

    public virtual AppUser Owner { get; set; } = null!;
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
