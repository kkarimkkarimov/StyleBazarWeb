using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

public class ProductSize : Entity<int>
{
    public int ProductId { get; set; }
    public required string Size { get; set; }
    public int StockQuantity { get; set; }

    public virtual Product Product { get; set; } = null!;
}
