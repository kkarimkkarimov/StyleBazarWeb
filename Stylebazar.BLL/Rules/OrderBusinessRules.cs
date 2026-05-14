using Stylebazar.BLL.Constants;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Rules;

public class OrderBusinessRules
{
    private readonly IOrderRepository _orders;
    private readonly IAddressRepository _addresses;

    public OrderBusinessRules(IOrderRepository orders, IAddressRepository addresses)
    {
        _orders = orders;
        _addresses = addresses;
    }

    public async Task<Order> OrderMustExistAsync(int id, CancellationToken ct = default)
    {
        Order? o = await _orders.GetAsync(x => x.Id == id, enableTracking: false, cancellationToken: ct);
        if (o is null) throw new NotFoundException(Messages.Order.NotFound);
        return o;
    }

    public void MustBeOwnerOrAdmin(Order order, string? currentUserId, bool isAdmin)
    {
        if (isAdmin) return;
        if (string.IsNullOrEmpty(currentUserId))
            throw new UnauthorizedException(Messages.Order.NotAuthenticated);
        if (order.UserId != currentUserId)
            throw new BusinessException(Messages.Order.NotOwner);
    }

    public async Task<Address> AddressMustBelongToUserAsync(int addressId, string userId, CancellationToken ct = default)
    {
        Address? a = await _addresses.GetAsync(x => x.Id == addressId, enableTracking: false, cancellationToken: ct);
        if (a is null) throw new NotFoundException(Messages.Order.AddressNotFound);
        if (a.UserId != userId)
            throw new BusinessException(Messages.Order.AddressNotYours);
        return a;
    }

    /// <summary>
    /// Validates that a target status is a legal transition from the current
    /// status. Lets admin/seller advance forward, cancel before delivery, or
    /// mark Returned only after Delivered.
    /// </summary>
    public void StatusTransitionMustBeValid(OrderStatus from, OrderStatus to)
    {
        if (from == to) return; // idempotent no-op

        bool allowed = (from, to) switch
        {
            (OrderStatus.Pending, OrderStatus.Confirmed) => true,
            (OrderStatus.Confirmed, OrderStatus.Processing) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            (OrderStatus.Delivered, OrderStatus.Returned) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            (OrderStatus.Processing, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed)
            throw new BusinessException(string.Format(Messages.Order.InvalidStatusTransition, from, to));
    }
}
