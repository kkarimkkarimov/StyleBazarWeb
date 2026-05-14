namespace Stylebazar.BLL.DTOs.Cart;

public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string? MainImageUrl { get; set; }

    public int? ProductSizeId { get; set; }
    public string? SizeLabel { get; set; }

    public int Quantity { get; set; }

    /// <summary>Price the user saw at add-time.</summary>
    public decimal UnitPriceSnapshot { get; set; }

    /// <summary>Live price now (Product.DiscountPrice ?? Product.Price).</summary>
    public decimal UnitPriceCurrent { get; set; }

    /// <summary>True if the live price differs from the snapshot.</summary>
    public bool PriceChanged => UnitPriceCurrent != UnitPriceSnapshot;

    public decimal LineTotal => UnitPriceCurrent * Quantity;

    public string StoreName { get; set; } = string.Empty;
    public int StoreId { get; set; }

    public int AvailableStock { get; set; }
    public bool IsActive { get; set; }
}
