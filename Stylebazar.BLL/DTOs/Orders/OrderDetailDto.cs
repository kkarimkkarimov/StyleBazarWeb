using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Orders;

// OrderDetailDto — bir sifarişin tam detalı: status, ödəniş, çatdırılma ünvanı (snapshot), sətirlər.
public class OrderDetailDto
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public OrderStatus Status { get; set; }
    public bool IsPaid { get; set; }
    public string? PaymentReference { get; set; }
    public decimal TotalAmount { get; set; }

    // Çatdırılma ünvanı (snapshot)
    public string ShipFullName { get; set; } = string.Empty;
    public string ShipPhone { get; set; } = string.Empty;
    public string ShipCity { get; set; } = string.Empty;
    public string? ShipDistrict { get; set; }
    public string ShipAddressLine { get; set; } = string.Empty;
    public string? ShipPostalCode { get; set; }
    public string? ShipNotes { get; set; }

    // Status izləmə tarixləri
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public List<OrderItemDto> Items { get; set; } = new();

    public int TotalQuantity => Items.Sum(i => i.Quantity);
}
