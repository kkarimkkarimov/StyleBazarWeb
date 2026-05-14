namespace Stylebazar.BLL.DTOs.Orders;

public class OrderItemDto
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public int? ProductSizeId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string? SizeLabel { get; set; }
    public int? StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
