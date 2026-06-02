using Stylebazar.BLL.Constants;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Rules;

// StoreBusinessRules — müvafiq əməliyyatlardan əvvəl yoxlanılan BİZNES QAYDALARI. Hər metod bir şərti yoxlayır; pozularsa xəta (BusinessException/NotFound) atır.
public class StoreBusinessRules
{
    private readonly IStoreRepository _stores;
    private readonly IProductRepository _products;

    public StoreBusinessRules(IStoreRepository stores, IProductRepository products)
    {
        _stores = stores;
        _products = products;
    }

    public async Task SlugMustBeUniqueAsync(string slug, int? excludeId = null, CancellationToken ct = default)
    {
        bool exists = await _stores.AnyAsync(
            s => s.Slug == slug && (!excludeId.HasValue || s.Id != excludeId.Value),
            enableTracking: false,
            cancellationToken: ct);

        if (exists)
            throw new BusinessException(Messages.Store.SlugAlreadyExists);
    }

    public async Task<Store> StoreMustExistAsync(int id, CancellationToken ct = default)
    {
        Store? store = await _stores.GetAsync(
            s => s.Id == id,
            enableTracking: false,
            cancellationToken: ct);

        if (store is null)
            throw new NotFoundException(nameof(Store), id);

        return store;
    }

    public void MustBeOwner(Store store, string? currentUserId)
    {
        if (string.IsNullOrEmpty(currentUserId))
            throw new UnauthorizedException(Messages.Store.OwnerNotAuthenticated);

        if (store.OwnerId != currentUserId)
            throw new BusinessException(Messages.Store.NotOwner);
    }

    public async Task CannotDeleteWhenHasProductsAsync(int storeId, CancellationToken ct = default)
    {
        bool hasProducts = await _products.AnyAsync(
            p => p.StoreId == storeId,
            enableTracking: false,
            cancellationToken: ct);

        if (hasProducts)
            throw new BusinessException(Messages.Store.HasProducts);
    }
}
