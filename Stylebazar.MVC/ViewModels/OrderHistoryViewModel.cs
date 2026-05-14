using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Orders;

namespace Stylebazar.MVC.ViewModels;

public class OrderHistoryViewModel
{
    public Paginate<OrderListDto> Orders { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
