namespace Stylebazar.BLL.DTOs.Orders;

// OrderItemDto — sifarişdəki bir məhsul sətri (oxu tərəfi). Snapshot dəyərləri ilə.
public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSlug { get; set; }
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public decimal LineTotal => UnitPrice * Quantity;   // sətir cəmi (hesablanır)
}
