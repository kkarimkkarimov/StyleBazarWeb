namespace Stylebazar.BLL.DTOs.Cart;

// CartSummaryDto — bütün səbət: elementlər + ümumi say + yekun məbləğ.
public class CartSummaryDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public int TotalQuantity => Items.Sum(i => i.Quantity);
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public bool IsEmpty => Items.Count == 0;
}
