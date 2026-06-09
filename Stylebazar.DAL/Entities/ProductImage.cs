// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// MƏHSUL ŞƏKLİ entity-si — "ProductImages" cədvəli. Bir məhsulun bir neçə şəkli olur.
public class ProductImage : Entity<int>
{
    // Hansı məhsula aiddir — xarici açar (FK → Product).
    public int ProductId { get; set; }
    // Şəklin URL/yolu. Məcburidir.
    public required string ImageUrl { get; set; }
    // Əsas şəkildirmi? true → məhsul kartlarında bu şəkil göstərilir.
    public bool IsMain { get; set; }
    // Göstərilmə sırası — kiçik rəqəm öndə görünür.
    public int DisplayOrder { get; set; }

    // Aid olduğu məhsul.
    public virtual Product Product { get; set; } = null!;
}
