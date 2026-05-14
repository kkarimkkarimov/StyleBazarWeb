using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Cart;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Rules;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Cart;

public class CartManager : ICartService
{
    private readonly ICartRepository _carts;
    private readonly ICartItemRepository _items;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly CartBusinessRules _rules;
    private readonly IValidator<AddToCartDto> _addValidator;
    private readonly IValidator<UpdateCartItemDto> _updateValidator;

    public CartManager(
        ICartRepository carts,
        ICartItemRepository items,
        IMapper mapper,
        ICurrentUserService currentUser,
        CartBusinessRules rules,
        IValidator<AddToCartDto> addValidator,
        IValidator<UpdateCartItemDto> updateValidator)
    {
        _carts = carts;
        _items = items;
        _mapper = mapper;
        _currentUser = currentUser;
        _rules = rules;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
    }

    public async Task<CartDto> GetMineAsync(CancellationToken ct = default)
    {
        DAL.Entities.Cart cart = await GetOrCreateCartAsync(ct);
        return await BuildDtoAsync(cart.Id, ct);
    }

    public async Task<CartDto> AddAsync(AddToCartDto dto, CancellationToken ct = default)
    {
        ValidationResult v = await _addValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        Product product = await _rules.ProductMustExistAndBeActiveAsync(dto.ProductId, ct);
        ProductSize? size = _rules.ValidateSizeChoice(product, dto.ProductSizeId);

        DAL.Entities.Cart cart = await GetOrCreateCartAsync(ct);

        // Merge into existing line if same product + size
        CartItem? existing = await _items.GetAsync(
            i => i.CartId == cart.Id
              && i.ProductId == dto.ProductId
              && i.ProductSizeId == (size != null ? size.Id : (int?)null),
            cancellationToken: ct);

        int unitPrice = 0;
        decimal effectivePrice = product.DiscountPrice ?? product.Price;

        if (existing is not null)
        {
            int newQty = existing.Quantity + dto.Quantity;
            _rules.MustHaveStockFor(product, size, newQty);
            existing.Quantity = newQty;
            existing.UnitPriceSnapshot = effectivePrice;
            await _items.UpdateAsync(existing, ct);
        }
        else
        {
            _rules.MustHaveStockFor(product, size, dto.Quantity);
            CartItem item = new()
            {
                CartId = cart.Id,
                ProductId = dto.ProductId,
                ProductSizeId = size?.Id,
                Quantity = dto.Quantity,
                UnitPriceSnapshot = effectivePrice
            };
            await _items.AddAsync(item, ct);
        }

        return await BuildDtoAsync(cart.Id, ct);
    }

    public async Task<CartDto> UpdateQuantityAsync(int itemId, UpdateCartItemDto dto, CancellationToken ct = default)
    {
        ValidationResult v = await _updateValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        DAL.Entities.Cart cart = await GetOrCreateCartAsync(ct);

        CartItem? line = await _items.GetAsync(
            i => i.Id == itemId && i.CartId == cart.Id,
            include: q => q.Include(i => i.Product).ThenInclude(p => p.Sizes),
            cancellationToken: ct);

        if (line is null)
            throw new NotFoundException(Messages.Cart.ItemNotFound);

        ProductSize? size = line.ProductSizeId.HasValue
            ? line.Product.Sizes.FirstOrDefault(s => s.Id == line.ProductSizeId.Value)
            : null;
        _rules.MustHaveStockFor(line.Product, size, dto.Quantity);

        line.Quantity = dto.Quantity;
        await _items.UpdateAsync(line, ct);

        return await BuildDtoAsync(cart.Id, ct);
    }

    public async Task<CartDto> RemoveAsync(int itemId, CancellationToken ct = default)
    {
        DAL.Entities.Cart cart = await GetOrCreateCartAsync(ct);

        CartItem? line = await _items.GetAsync(
            i => i.Id == itemId && i.CartId == cart.Id,
            cancellationToken: ct);

        if (line is null)
            throw new NotFoundException(Messages.Cart.ItemNotFound);

        await _items.DeleteAsync(line, permanent: true, cancellationToken: ct);
        return await BuildDtoAsync(cart.Id, ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        DAL.Entities.Cart cart = await GetOrCreateCartAsync(ct);

        Paginate<CartItem> page = await _items.GetListAsync(
            predicate: i => i.CartId == cart.Id,
            index: 0,
            size: int.MaxValue,
            enableTracking: true,
            cancellationToken: ct);

        foreach (CartItem item in page.Items)
            await _items.DeleteAsync(item, permanent: true, cancellationToken: ct);
    }

    public async Task<int> GetMyCountAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return 0;

        string userId = _currentUser.UserId;
        DAL.Entities.Cart? cart = await _carts.GetAsync(
            c => c.UserId == userId,
            include: q => q.Include(c => c.Items),
            enableTracking: false,
            cancellationToken: ct);

        return cart is null ? 0 : cart.Items.Sum(i => i.Quantity);
    }

    // ----- helpers -----

    private string RequireUserId()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Cart.NotAuthenticated);
        return _currentUser.UserId;
    }

    private async Task<DAL.Entities.Cart> GetOrCreateCartAsync(CancellationToken ct)
    {
        string userId = RequireUserId();

        DAL.Entities.Cart? cart = await _carts.GetAsync(
            c => c.UserId == userId,
            cancellationToken: ct);

        if (cart is not null) return cart;

        DAL.Entities.Cart created = await _carts.AddAsync(
            new DAL.Entities.Cart { UserId = userId },
            ct);
        return created;
    }

    /// <summary>
    /// Loads the cart with full includes, then maps with live product data so
    /// the UI can show "price changed" / "out of stock" warnings.
    /// </summary>
    private async Task<CartDto> BuildDtoAsync(int cartId, CancellationToken ct)
    {
        DAL.Entities.Cart? cart = await _carts.GetAsync(
            c => c.Id == cartId,
            include: q => q
                .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Store)
                .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
                .Include(c => c.Items).ThenInclude(i => i.ProductSize),
            enableTracking: false,
            cancellationToken: ct);

        if (cart is null) return new CartDto();

        CartDto dto = new() { Id = cart.Id };
        foreach (CartItem line in cart.Items.OrderByDescending(i => i.CreatedDate))
        {
            decimal currentUnit = line.Product.DiscountPrice ?? line.Product.Price;
            int availableStock = line.ProductSize is not null
                ? line.ProductSize.StockQuantity
                : line.Product.StockQuantity;

            string? mainImage = line.Product.Images
                .Where(i => i.IsMain)
                .Select(i => i.ImageUrl)
                .FirstOrDefault()
              ?? line.Product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .FirstOrDefault();

            dto.Items.Add(new CartItemDto
            {
                Id = line.Id,
                ProductId = line.ProductId,
                ProductName = line.Product.Name,
                ProductSlug = line.Product.Slug,
                MainImageUrl = mainImage,
                ProductSizeId = line.ProductSizeId,
                SizeLabel = line.ProductSize?.Size,
                Quantity = line.Quantity,
                UnitPriceSnapshot = line.UnitPriceSnapshot,
                UnitPriceCurrent = currentUnit,
                StoreId = line.Product.StoreId,
                StoreName = line.Product.Store?.Name ?? string.Empty,
                AvailableStock = availableStock,
                IsActive = line.Product.IsActive
            });
        }

        return dto;
    }
}
