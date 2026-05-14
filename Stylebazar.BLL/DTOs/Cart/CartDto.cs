namespace Stylebazar.BLL.DTOs.Cart;

public class CartDto
{
    public int Id { get; set; }
    public List<CartItemDto> Items { get; set; } = new();

    public int TotalItemCount => Items.Sum(i => i.Quantity);
    public decimal Subtotal => Items.Sum(i => i.LineTotal);

    /// <summary>True if any item has a stale price or insufficient stock.</summary>
    public bool HasIssues => Items.Any(i => i.PriceChanged || i.Quantity > i.AvailableStock || !i.IsActive);
}
