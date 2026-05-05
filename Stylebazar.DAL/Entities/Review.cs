using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

public class Review : Entity<int>
{
    public required string UserId { get; set; }
    public int ProductId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    public virtual AppUser User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
}
