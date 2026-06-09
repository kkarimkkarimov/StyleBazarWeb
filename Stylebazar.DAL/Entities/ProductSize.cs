// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// MƏHSUL ÖLÇÜSÜ entity-si — "ProductSizes" cədvəli. Hər ölçü üçün ayrıca anbar sayı saxlanır.
public class ProductSize : Entity<int>
{
    // Hansı məhsula aiddir — xarici açar (FK → Product).
    public int ProductId { get; set; }
    // Ölçü adı: "S", "M", "L", "42". Məcburidir.
    public required string Size { get; set; }
    // Bu ölçüdən anbarda neçə ədəd var.
    public int StockQuantity { get; set; }

    // Aid olduğu məhsul.
    public virtual Product Product { get; set; } = null!;
}
