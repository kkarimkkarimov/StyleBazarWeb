using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Orders;

namespace Stylebazar.BLL.Services.Orders;

public interface IOrderService
{
    /// <summary>
    /// Place a new order from the current user's cart. Re-validates stock,
    /// snapshots product + address fields, decrements stock, empties the cart.
    /// </summary>
    Task<OrderDto> PlaceFromCartAsync(PlaceOrderDto dto, CancellationToken ct = default);

    /// <summary>Paginated history of the current user's orders, newest first.</summary>
    Task<Paginate<OrderListDto>> GetMineAsync(int page, int pageSize, CancellationToken ct = default);

    /// <summary>Full order detail. Owner or admin only.</summary>
    Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Update status (admin / seller-of-store). Validates state machine.</summary>
    Task<OrderDto> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, CancellationToken ct = default);

    /// <summary>Buyer can cancel their own order while it's pre-shipping.</summary>
    Task<OrderDto> CancelMineAsync(int id, CancellationToken ct = default);
}
