using AutoMapper;
using Core.Persistence.Paging;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Wishlist;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Services.Wishlist;

public class WishlistManager : IWishlistService
{
    private readonly IWishlistRepository _repo;
    private readonly IProductRepository _products;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public WishlistManager(
        IWishlistRepository repo,
        IProductRepository products,
        IMapper mapper,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _products = products;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<List<WishlistItemDto>> GetMineAsync(CancellationToken ct = default)
    {
        string userId = RequireUserId();

        Paginate<DAL.Entities.Wishlist> page = await _repo.GetListAsync(
            predicate: w => w.UserId == userId,
            orderBy: q => q.OrderByDescending(w => w.CreatedDate),
            include: q => q
                .Include(w => w.Product).ThenInclude(p => p.Store)
                .Include(w => w.Product).ThenInclude(p => p.Images),
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);

        return _mapper.Map<List<WishlistItemDto>>(page.Items);
    }

    public async Task<WishlistItemDto> AddAsync(int productId, CancellationToken ct = default)
    {
        string userId = RequireUserId();

        // Product must exist (and be active for adding to wishlist).
        Product? product = await _products.GetAsync(
            p => p.Id == productId,
            enableTracking: false,
            cancellationToken: ct);
        if (product is null)
            throw new NotFoundException(nameof(Product), productId);

        bool already = await _repo.AnyAsync(
            w => w.UserId == userId && w.ProductId == productId,
            enableTracking: false,
            cancellationToken: ct);
        if (already)
            throw new BusinessException(Messages.Wishlist.ProductAlreadyInList);

        DAL.Entities.Wishlist entity = new()
        {
            UserId = userId,
            ProductId = productId
        };
        DAL.Entities.Wishlist created = await _repo.AddAsync(entity, ct);

        // Reload with includes for the response.
        DAL.Entities.Wishlist? full = await _repo.GetAsync(
            w => w.Id == created.Id,
            include: q => q
                .Include(w => w.Product).ThenInclude(p => p.Store)
                .Include(w => w.Product).ThenInclude(p => p.Images),
            enableTracking: false,
            cancellationToken: ct);

        return _mapper.Map<WishlistItemDto>(full ?? created);
    }

    public async Task<HashSet<int>> GetMyProductIdsAsync(CancellationToken ct = default)
    {
        // Anonymous callers — no wishlist, no exception. Lets call sites
        // always pass us the user context without guarding.
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return new HashSet<int>();

        string userId = _currentUser.UserId;
        Paginate<DAL.Entities.Wishlist> page = await _repo.GetListAsync(
            predicate: w => w.UserId == userId,
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);

        return page.Items.Select(w => w.ProductId).ToHashSet();
    }

    public async Task<int> GetMyCountAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return 0;
        string userId = _currentUser.UserId;
        // size: 1 → server runs COUNT(*) + TOP 1, we only read .Count.
        Paginate<DAL.Entities.Wishlist> page = await _repo.GetListAsync(
            predicate: w => w.UserId == userId,
            index: 0,
            size: 1,
            enableTracking: false,
            cancellationToken: ct);
        return page.Count;
    }

    public async Task RemoveAsync(int productId, CancellationToken ct = default)
    {
        string userId = RequireUserId();

        DAL.Entities.Wishlist? tracked = await _repo.GetAsync(
            w => w.UserId == userId && w.ProductId == productId,
            cancellationToken: ct);

        if (tracked is null)
            throw new NotFoundException(Messages.Wishlist.ItemNotFound);

        await _repo.DeleteAsync(tracked, permanent: true, cancellationToken: ct);
    }

    private string RequireUserId()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Wishlist.NotAuthenticated);
        return _currentUser.UserId;
    }
}
