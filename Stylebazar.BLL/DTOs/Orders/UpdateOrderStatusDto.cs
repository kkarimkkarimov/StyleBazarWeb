using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Orders;

public class UpdateOrderStatusDto
{
    public OrderStatus Status { get; set; }
}
