using Stylebazar.BLL.Constants;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Rules;

public class ProductBusinessRules
{
    private readonly IProductRepository _products;
    private readonly IStoreRepository _stores;
    private readonly ICategoryRepository _categories;

    public ProductBusinessRules(
        IProductRepository products,
        IStoreRepository stores,
        ICategoryRepository categories)
    {
        _products = products;
        _stores = stores;
        _categories = categories;
    }

    public async Task SlugMustBeUniqueAsync(string slug, int? excludeId = null, CancellationToken ct = default)
    {
        bool exists = await _products.AnyAsync(
            p => p.Slug == slug && (!excludeId.HasValue || p.Id != excludeId.Value),
            enableTracking: false,
            cancellationToken: ct);

        if (exists)
            throw new BusinessException(Messages.Product.SlugAlreadyExists);
    }

    public async Task<Product> ProductMustExistAsync(int id, CancellationToken ct = default)
    {
        Product? product = await _products.GetAsync(
            p => p.Id == id,
            enableTracking: false,
            cancellationToken: ct);

        if (product is null)
            throw new NotFoundException(nameof(Product), id);

        return product;
    }

    public async Task<Store> StoreMustExistAndBeActiveAsync(int storeId, CancellationToken ct = default)
    {
        Store? store = await _stores.GetAsync(
            s => s.Id == storeId,
            enableTracking: false,
            cancellationToken: ct);

        if (store is null)
            throw new NotFoundException(nameof(Store), storeId);

        if (!store.IsActive)
            throw new BusinessException(Messages.Store.Inactive);

        return store;
    }

    /// <summary>
    /// Verifies the store exists but does NOT require it to be active. Used by
    /// teardown/management operations (product delete, activation toggle): these
    /// must succeed even after a store is deactivated, otherwise a suspended
    /// store and its products become permanently undeletable (the store-delete
    /// guard blocks while products exist, and product-delete would block while
    /// the store is inactive). Ownership is still enforced separately.
    /// </summary>
    public async Task<Store> StoreMustExistAsync(int storeId, CancellationToken ct = default)
    {
        Store? store = await _stores.GetAsync(
            s => s.Id == storeId,
            enableTracking: false,
            cancellationToken: ct);

        if (store is null)
            throw new NotFoundException(nameof(Store), storeId);

        return store;
    }

    public async Task CategoryMustExistAsync(int categoryId, CancellationToken ct = default)
    {
        bool exists = await _categories.AnyAsync(
            c => c.Id == categoryId,
            enableTracking: false,
            cancellationToken: ct);

        if (!exists)
            throw new NotFoundException(nameof(Category), categoryId);
    }

    public void MustOwnStore(Store store, string? currentUserId, bool isAdmin)
    {
        if (isAdmin) return;

        if (string.IsNullOrEmpty(currentUserId))
            throw new UnauthorizedException(Messages.Store.OwnerNotAuthenticated);

        if (store.OwnerId != currentUserId)
            throw new BusinessException(Messages.Product.NotStoreOwner);
    }

    public void DiscountMustBeLowerThanPrice(decimal price, decimal? discountPrice)
    {
        if (discountPrice.HasValue && discountPrice.Value >= price)
            throw new BusinessException(Messages.Product.DiscountTooHigh);
    }
}
