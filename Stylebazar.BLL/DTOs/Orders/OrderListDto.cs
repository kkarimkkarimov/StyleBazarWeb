using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Orders;

/// <summary>Light-weight DTO used by the order history list page.</summary>
public class OrderListDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public string? FirstItemImageUrl { get; set; }
    public string FirstItemName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
