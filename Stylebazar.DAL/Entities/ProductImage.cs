using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// MƏHSUL ŞƏKLİ entity-si — "ProductImages" cədvəli. Bir məhsulun bir neçə şəkli olur.
public class ProductImage : Entity<int>
{
    public int ProductId { get; set; }              // Hansı məhsula aiddir (FK → Product)
    public required string ImageUrl { get; set; }    // Şəklin URL/yolu (məcburi)
    public bool IsMain { get; set; }                 // Əsas şəkildir? (kartlarda bu göstərilir)
    public int DisplayOrder { get; set; }            // Göstərilmə sırası (kiçik rəqəm öndə)

    public virtual Product Product { get; set; } = null!;  // Aid olduğu məhsul
}
