// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// MƏHSUL RƏNGİ entity-si — "ProductColors" cədvəli. Detal səhifəsində dairə (swatch) kimi göstərilir;
// rəngə klikləyəndə WhatsApp mesajı yenilənir ki, satıcı hansı rəngin soruşulduğunu bilsin.
public class ProductColor : Entity<int>
{
    // Hansı məhsula aiddir — xarici açar (FK → Product).
    public int ProductId { get; set; }
    // Rəngin adı: "Çəhrayı", "Açıq mavi". Məcburidir.
    public required string Name { get; set; }
    // Hex kodu (# ilə, məs. "#F8C2C2") — swatch dairəsinin rəngini verir. Məcburidir.
    public required string Hex { get; set; }
    // Göstərilmə sırası — kiçik rəqəm öndə.
    public int DisplayOrder { get; set; }

    // Aid olduğu məhsul.
    public virtual Product Product { get; set; } = null!;
}
