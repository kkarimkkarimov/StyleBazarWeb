using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Orders;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Rules;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Orders;

public class OrderManager : IOrderService
{
    // MVP-flat shipping fee. Phase 5c will move this behind a
    // DeliveryMethod table + a strategy service.
    private const decimal FlatShippingFee = 5.00m;

    private readonly IOrderRepository _orders;
    private readonly IOrderItemRepository _items;
    private readonly ICartRepository _carts;
    private readonly ICartItemRepository _cartItems;
    private readonly IProductRepository _products;
    private readonly IProductSizeRepository _sizes;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly OrderBusinessRules _rules;
    private readonly IValidator<PlaceOrderDto> _placeValidator;

    public OrderManager(
        IOrderRepository orders,
        IOrderItemRepository items,
        ICartRepository carts,
        ICartItemRepository cartItems,
        IProductRepository products,
        IProductSizeRepository sizes,
        IMapper mapper,
        ICurrentUserService currentUser,
        OrderBusinessRules rules,
        IValidator<PlaceOrderDto> placeValidator)
    {
        _orders = orders;
        _items = items;
        _carts = carts;
        _cartItems = cartItems;
        _products = products;
        _sizes = sizes;
        _mapper = mapper;
        _currentUser = currentUser;
        _rules = rules;
        _placeValidator = placeValidator;
    }

    public async Task<OrderDto> PlaceFromCartAsync(PlaceOrderDto dto, CancellationToken ct = default)
    {
        string userId = RequireUserId();

        ValidationResult v = await _placeValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        // 1. Address ownership check + snapshot fields
        Address address = await _rules.AddressMustBelongToUserAsync(dto.AddressId, userId, ct);

        // 2. Load cart with everything we need
        DAL.Entities.Cart? cart = await _carts.GetAsync(
            c => c.UserId == userId,
            include: q => q
                .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Store)
                .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
                .Include(c => c.Items).ThenInclude(i => i.ProductSize),
            enableTracking: false,
            cancellationToken: ct);

        if (cart is null || cart.Items.Count == 0)
            throw new BusinessException(Messages.Order.CartEmpty);

        // 3. Re-validate every line — stock + active. Collect violations into
        //    a single exception so the UI can list them all at once.
        List<string> violations = new();
        foreach (CartItem line in cart.Items)
        {
            if (!line.Product.IsActive)
                violations.Add(string.Format(Messages.Order.LineInactive, line.Product.Name));
            int available = line.ProductSize is not null
                ? line.ProductSize.StockQuantity
                : line.Product.StockQuantity;
            if (line.Quantity > available)
                violations.Add(string.Format(Messages.Order.LineOutOfStock, line.Product.Name, available));
        }
        if (violations.Count > 0)
            throw new BusinessException(string.Join(" • ", violations));

        // 4. Compute totals from LIVE prices (DiscountPrice ?? Price)
        decimal subtotal = 0m;
        foreach (CartItem line in cart.Items)
        {
            decimal unit = line.Product.DiscountPrice ?? line.Product.Price;
            subtotal += unit * line.Quantity;
        }
        decimal shipping = FlatShippingFee;
        decimal total = subtotal + shipping;

        // 5. Generate a human-readable order number ("SB-2026-000042")
        string orderNumber = $"SB-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(100000, 999999)}";

        // 6. Build Order header + items (snapshots)
        Order order = new()
        {
            UserId = userId,
            OrderNumber = orderNumber,
            Status = OrderStatus.Pending,
            PaymentMethod = dto.PaymentMethod,
            ShipFullName = address.FullName,
            ShipPhone = address.Phone,
            ShipCity = address.City,
            ShipAddressLine = address.AddressLine,
            ShipPostalCode = address.PostalCode,
            ShipNotes = address.Notes,
            Subtotal = subtotal,
            ShippingFee = shipping,
            Total = total,
            CustomerNote = string.IsNullOrWhiteSpace(dto.CustomerNote) ? null : dto.CustomerNote.Trim(),
            StatusChangedDate = DateTime.UtcNow
        };
        Order createdOrder = await _orders.AddAsync(order, ct);

        // 7. Snapshot each line + decrement stock
        foreach (CartItem line in cart.Items)
        {
            decimal unit = line.Product.DiscountPrice ?? line.Product.Price;
            string? mainImage = line.Product.Images
                .Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
              ?? line.Product.Images
                .OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault();

            OrderItem item = new()
            {
                OrderId = createdOrder.Id,
                ProductId = line.ProductId,
                ProductSizeId = line.ProductSizeId,
                ProductName = line.Product.Name,
                ProductImageUrl = mainImage,
                SizeLabel = line.ProductSize?.Size,
                StoreId = line.Product.StoreId,
                StoreName = line.Product.Store?.Name ?? string.Empty,
                Quantity = line.Quantity,
                UnitPrice = unit,
                LineTotal = unit * line.Quantity
            };
            await _items.AddAsync(item, ct);

            // 8. Decrement stock at the most specific level (size > product)
            if (line.ProductSizeId.HasValue)
            {
                ProductSize? sz = await _sizes.GetAsync(
                    s => s.Id == line.ProductSizeId.Value,
                    cancellationToken: ct);
                if (sz is not null)
                {
                    sz.StockQuantity = Math.Max(0, sz.StockQuantity - line.Quantity);
                    await _sizes.UpdateAsync(sz, ct);
                }
            }
            else
            {
                Product? p = await _products.GetAsync(
                    pp => pp.Id == line.ProductId,
                    cancellationToken: ct);
                if (p is not null)
                {
                    p.StockQuantity = Math.Max(0, p.StockQuantity - line.Quantity);
                    await _products.UpdateAsync(p, ct);
                }
            }
        }

        // 9. Empty the cart (permanent delete the lines)
        foreach (CartItem line in cart.Items)
        {
            CartItem? tracked = await _cartItems.GetAsync(i => i.Id == line.Id, cancellationToken: ct);
            if (tracked is not null)
                await _cartItems.DeleteAsync(tracked, permanent: true, cancellationToken: ct);
        }

        // 10. Return full DTO
        return await BuildDtoAsync(createdOrder.Id, ct);
    }

    public async Task<Paginate<OrderListDto>> GetMineAsync(int page, int pageSize, CancellationToken ct = default)
    {
        string userId = RequireUserId();

        Paginate<Order> p = await _orders.GetListAsync(
            predicate: o => o.UserId == userId,
            orderBy: q => q.OrderByDescending(o => o.CreatedDate),
            include: q => q.Include(o => o.Items),
            index: page - 1, size: pageSize,
            enableTracking: false, cancellationToken: ct);

        List<OrderListDto> list = p.Items.Select(o => new OrderListDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            PaymentMethod = o.PaymentMethod,
            Total = o.Total,
            ItemCount = o.Items.Sum(i => i.Quantity),
            FirstItemImageUrl = o.Items.OrderBy(i => i.Id).Select(i => i.ProductImageUrl).FirstOrDefault(),
            FirstItemName = o.Items.OrderBy(i => i.Id).Select(i => i.ProductName).FirstOrDefault() ?? string.Empty,
            CreatedDate = o.CreatedDate
        }).ToList();

        return new Paginate<OrderListDto>
        {
            Index = p.Index, Size = p.Size, Count = p.Count, Pages = p.Pages,
            Items = list
        };
    }

    public async Task<OrderDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Order existing = await _rules.OrderMustExistAsync(id, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustBeOwnerOrAdmin(existing, _currentUser.UserId, isAdmin);

        return await BuildDtoAsync(id, ct);
    }

    public async Task<OrderDto> UpdateStatusAsync(int id, UpdateOrderStatusDto dto, CancellationToken ct = default)
    {
        Order existing = await _rules.OrderMustExistAsync(id, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        bool isSeller = _currentUser.IsInRole("Seller");

        // Only Admin can flip any status; Seller can advance their store's
        // orders (in 5c we narrow this to only orders containing their items).
        if (!isAdmin && !isSeller)
            throw new BusinessException(Messages.Order.NotAuthorisedToUpdate);

        _rules.StatusTransitionMustBeValid(existing.Status, dto.Status);

        Order? tracked = await _orders.GetAsync(o => o.Id == id, cancellationToken: ct);
        if (tracked is null) throw new NotFoundException(Messages.Order.NotFound);
        tracked.Status = dto.Status;
        tracked.StatusChangedDate = DateTime.UtcNow;
        await _orders.UpdateAsync(tracked, ct);

        return await BuildDtoAsync(id, ct);
    }

    public async Task<OrderDto> CancelMineAsync(int id, CancellationToken ct = default)
    {
        Order existing = await _rules.OrderMustExistAsync(id, ct);
        _rules.MustBeOwnerOrAdmin(existing, _currentUser.UserId, isAdmin: false);

        // Buyer can cancel only while pre-shipping.
        if (existing.Status == OrderStatus.Shipped
            || existing.Status == OrderStatus.Delivered
            || existing.Status == OrderStatus.Cancelled
            || existing.Status == OrderStatus.Returned)
        {
            throw new BusinessException(Messages.Order.CannotCancelAtThisStage);
        }

        Order? tracked = await _orders.GetAsync(o => o.Id == id, cancellationToken: ct);
        if (tracked is null) throw new NotFoundException(Messages.Order.NotFound);
        tracked.Status = OrderStatus.Cancelled;
        tracked.StatusChangedDate = DateTime.UtcNow;
        await _orders.UpdateAsync(tracked, ct);

        return await BuildDtoAsync(id, ct);
    }

    // -------- helpers --------

    private string RequireUserId()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Order.NotAuthenticated);
        return _currentUser.UserId;
    }

    private async Task<OrderDto> BuildDtoAsync(int orderId, CancellationToken ct)
    {
        Order? o = await _orders.GetAsync(
            x => x.Id == orderId,
            include: q => q.Include(x => x.Items),
            enableTracking: false, cancellationToken: ct);
        if (o is null) throw new NotFoundException(Messages.Order.NotFound);

        return new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            PaymentMethod = o.PaymentMethod,
            ShipFullName = o.ShipFullName,
            ShipPhone = o.ShipPhone,
            ShipCity = o.ShipCity,
            ShipAddressLine = o.ShipAddressLine,
            ShipPostalCode = o.ShipPostalCode,
            ShipNotes = o.ShipNotes,
            Subtotal = o.Subtotal,
            ShippingFee = o.ShippingFee,
            Total = o.Total,
            CustomerNote = o.CustomerNote,
            CreatedDate = o.CreatedDate,
            StatusChangedDate = o.StatusChangedDate,
            Items = o.Items.OrderBy(i => i.Id).Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductSizeId = i.ProductSizeId,
                ProductName = i.ProductName,
                ProductImageUrl = i.ProductImageUrl,
                SizeLabel = i.SizeLabel,
                StoreId = i.StoreId,
                StoreName = i.StoreName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }
}
