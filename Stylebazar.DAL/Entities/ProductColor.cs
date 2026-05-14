using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// A color a product is available in. Rendered as a circle swatch on the
/// detail page; clicking one updates the WhatsApp pre-filled message so the
/// seller knows which colour the buyer is asking about.
/// </summary>
public class ProductColor : Entity<int>
{
    public int ProductId { get; set; }

    /// <summary>Display name in Az ("Çəhrayı", "Açıq mavi").</summary>
    public required string Name { get; set; }

    /// <summary>Hex code with leading # (e.g. "#F8C2C2"). Drives the swatch fill.</summary>
    public required string Hex { get; set; }

    public int DisplayOrder { get; set; }

    public virtual Product Product { get; set; } = null!;
}
