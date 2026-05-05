using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

public class ReviewImage : Entity<int>
{
    public int ReviewId { get; set; }
    public required string ImageUrl { get; set; }

    public virtual Review Review { get; set; } = null!;
}
