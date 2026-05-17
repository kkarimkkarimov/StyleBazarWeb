using Core.Persistence.Paging;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Rules;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Services.StoreFollowers;

public class StoreFollowerManager : IStoreFollowerService
{
    private readonly IStoreFollowerRepository _follows;
    private readonly StoreBusinessRules _storeRules;
    private readonly ICurrentUserService _currentUser;

    public StoreFollowerManager(
        IStoreFollowerRepository follows,
        StoreBusinessRules storeRules,
        ICurrentUserService currentUser)
    {
        _follows = follows;
        _storeRules = storeRules;
        _currentUser = currentUser;
    }

    public async Task<FollowResult> FollowAsync(int storeId, CancellationToken ct = default)
    {
        string userId = RequireUserId();
        await _storeRules.StoreMustExistAsync(storeId, ct);

        // Idempotent — already following is a no-op success.
        StoreFollower? existing = await _follows.GetAsync(
            f => f.UserId == userId && f.StoreId == storeId,
            enableTracking: false,
            cancellationToken: ct);

        if (existing is null)
        {
            await _follows.AddAsync(new StoreFollower
            {
                UserId = userId,
                StoreId = storeId
            }, ct);
        }

        int count = await GetFollowerCountAsync(storeId, ct);
        return new FollowResult { IsFollowing = true, FollowerCount = count };
    }

    public async Task<FollowResult> UnfollowAsync(int storeId, CancellationToken ct = default)
    {
        string userId = RequireUserId();
        await _storeRules.StoreMustExistAsync(storeId, ct);

        StoreFollower? existing = await _follows.GetAsync(
            f => f.UserId == userId && f.StoreId == storeId,
            cancellationToken: ct);

        if (existing is not null)
            await _follows.DeleteAsync(existing, permanent: true, cancellationToken: ct);

        int count = await GetFollowerCountAsync(storeId, ct);
        return new FollowResult { IsFollowing = false, FollowerCount = count };
    }

    public async Task<bool> IsFollowedByCurrentUserAsync(int storeId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId)) return false;
        string userId = _currentUser.UserId;

        return await _follows.AnyAsync(
            f => f.UserId == userId && f.StoreId == storeId,
            enableTracking: false,
            cancellationToken: ct);
    }

    public async Task<int> GetFollowerCountAsync(int storeId, CancellationToken ct = default)
    {
        // size:1 → server runs COUNT(*) + TOP 1; we only read .Count.
        Paginate<StoreFollower> page = await _follows.GetListAsync(
            predicate: f => f.StoreId == storeId,
            orderBy: q => q.OrderBy(f => f.Id),
            index: 0, size: 1,
            enableTracking: false,
            cancellationToken: ct);
        return page.Count;
    }

    private string RequireUserId()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.StoreFollower.NotAuthenticated);
        return _currentUser.UserId;
    }
}
