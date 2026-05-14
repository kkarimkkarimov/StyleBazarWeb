using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// A line in an order. We keep ProductId as a nullable FK so a product can be
/// deleted (soft) without orphaning history — the OrderItem still renders via
/// its snapshot fields. SizeLabel / ProductName / ImageUrl are all captured
/// at order-time so reprints look identical to the original receipt.
/// </summary>
public class OrderItem : Entity<int>
{
    public int OrderId { get; set; }

    /// <summary>Nullable so soft-deletes of products don't break history.</summary>
    public int? ProductId { get; set; }
    public int? ProductSizeId { get; set; }

    // --- snapshot ---
    public required string ProductName { get; set; }
    public string? ProductImageUrl { get; set; }
    public string? SizeLabel { get; set; }
    public required string StoreName { get; set; }
    public int? StoreId { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public virtual Order Order { get; set; } = null!;
    public virtual Product? Product { get; set; }
    public virtual ProductSize? ProductSize { get; set; }
}
