using Stylebazar.BLL.DTOs.Orders;

namespace Stylebazar.MVC.ViewModels;

public class OrderDetailViewModel
{
    public OrderDto Order { get; set; } = new();
}
