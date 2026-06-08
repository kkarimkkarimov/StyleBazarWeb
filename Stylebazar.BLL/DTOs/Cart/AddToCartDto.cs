namespace Stylebazar.BLL.DTOs.Cart;

// AddToCartDto — "Səbətə at" əməliyyatı üçün məlumat (məhsul + say + seçilmiş ölçü/rəng).
public class AddToCartDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Size { get; set; }
    public string? Color { get; set; }
}
