namespace Stylebazar.BLL.DTOs.Cart;

public class AddToCartDto
{
    public int ProductId { get; set; }
    public int? ProductSizeId { get; set; }
    public int Quantity { get; set; } = 1;
}
