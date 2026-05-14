using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Orders;

public class PlaceOrderDto
{
    public int AddressId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;
    public string? CustomerNote { get; set; }
}
