using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Orders;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }

    public string ShipFullName { get; set; } = string.Empty;
    public string ShipPhone { get; set; } = string.Empty;
    public string ShipCity { get; set; } = string.Empty;
    public string ShipAddressLine { get; set; } = string.Empty;
    public string? ShipPostalCode { get; set; }
    public string? ShipNotes { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }

    public string? CustomerNote { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? StatusChangedDate { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();

    public int TotalItemCount => Items.Sum(i => i.Quantity);
}
