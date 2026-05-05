using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

public class ProductImage : Entity<int>
{
    public int ProductId { get; set; }
    public required string ImageUrl { get; set; }
    public bool IsMain { get; set; }
    public int DisplayOrder { get; set; }

    public virtual Product Product { get; set; } = null!;
}
