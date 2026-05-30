using Stylebazar.BLL.Constants;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Rules;

public class StoryBusinessRules
{
    private const int MaxActiveStoriesPerStore = 10;

    private readonly IStoryRepository _stories;
    private readonly IStoreRepository _stores;

    public StoryBusinessRules(IStoryRepository stories, IStoreRepository stores)
    {
        _stories = stories;
        _stores = stores;
    }

    public async Task<Story> StoryMustExistAsync(int id, CancellationToken ct = default)
    {
        Story? story = await _stories.GetAsync(
            s => s.Id == id,
            enableTracking: false,
            cancellationToken: ct);
        if (story is null)
            throw new NotFoundException(Messages.Story.NotFound);
        return story;
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
    /// Verifies the store exists without requiring it to be active. Used by
    /// story deletion so a story can still be removed after its store has been
    /// deactivated. Ownership is enforced separately (admin bypasses).
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

    public void MustBeOwner(Store store, string? currentUserId, bool isAdmin)
    {
        if (isAdmin) return;
        if (string.IsNullOrEmpty(currentUserId))
            throw new UnauthorizedException(Messages.Story.NotAuthenticated);
        if (store.OwnerId != currentUserId)
            throw new BusinessException(Messages.Story.NotStoreOwner);
    }

    public async Task EnsureUnderActiveLimitAsync(int storeId, CancellationToken ct = default)
    {
        DateTime now = DateTime.UtcNow;
        bool hasRoom = !await _stories.AnyAsync(
            s => s.StoreId == storeId && s.IsActive && s.ExpiresAt > now,
            enableTracking: false,
            cancellationToken: ct);
        if (hasRoom) return;

        // Count properly
        Core.Persistence.Paging.Paginate<Story> page = await _stories.GetListAsync(
            predicate: s => s.StoreId == storeId && s.IsActive && s.ExpiresAt > now,
            index: 0,
            size: 1,
            enableTracking: false,
            cancellationToken: ct);

        if (page.Count >= MaxActiveStoriesPerStore)
            throw new BusinessException(Messages.Story.TooManyActive);
    }
}
