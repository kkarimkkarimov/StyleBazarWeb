using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// MƏHSUL RƏNGİ entity-si — "ProductColors" cədvəli. Detal səhifəsində dairə (swatch) kimi
// göstərilir; rəngə klikləyəndə WhatsApp mesajı yenilənir ki, satıcı hansı rəng soruşulduğunu bilsin.
public class ProductColor : Entity<int>
{
    public int ProductId { get; set; }            // Hansı məhsula aiddir (FK → Product)

    public required string Name { get; set; }      // Rəngin adı: "Çəhrayı", "Açıq mavi"

    public required string Hex { get; set; }       // Hex kodu (# ilə, məs. "#F8C2C2") — swatch-ın rəngini verir

    public int DisplayOrder { get; set; }          // Göstərilmə sırası

    public virtual Product Product { get; set; } = null!;  // Aid olduğu məhsul
}
