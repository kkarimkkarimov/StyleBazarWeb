using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// MƏHSUL ÖLÇÜSÜ entity-si — "ProductSizes" cədvəli. Hər ölçü üçün ayrıca anbar sayı saxlanır.
public class ProductSize : Entity<int>
{
    public int ProductId { get; set; }            // Hansı məhsula aiddir (FK → Product)
    public required string Size { get; set; }      // Ölçü adı: "S", "M", "L", "42"
    public int StockQuantity { get; set; }         // Bu ölçüdən neçə ədəd var

    public virtual Product Product { get; set; } = null!;  // Aid olduğu məhsul
}
