using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// A line in the cart. We snapshot UnitPriceSnapshot at add-time but always
/// re-validate against the live Product.Price at checkout so a stale price
/// can't be locked in for cheap. SizeId is optional because some products
/// don't have sizes (e.g. accessories).
/// </summary>
public class CartItem : Entity<int>
{
    public int CartId { get; set; }
    public int ProductId { get; set; }

    /// <summary>Optional — only set when the product has sizes.</summary>
    public int? ProductSizeId { get; set; }

    public int Quantity { get; set; }

    /// <summary>
    /// Price the user saw when they added the item. Used to detect
    /// price-changed-since-add and warn the user at checkout.
    /// </summary>
    public decimal UnitPriceSnapshot { get; set; }

    public virtual Cart Cart { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ProductSize? ProductSize { get; set; }
}
