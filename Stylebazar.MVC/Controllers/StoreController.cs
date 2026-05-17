using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Products;
using Stylebazar.BLL.Services.Stories;
using Stylebazar.BLL.Services.StoreFollowers;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.BLL.Services.Wishlist;
using Stylebazar.MVC.ViewModels;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// Public shop catalog: list of all active sellers + a single store profile +
/// follow/unfollow endpoints.
/// </summary>
public class StoreController : Controller
{
    private readonly IStoreService _stores;
    private readonly IProductService _products;
    private readonly IStoryService _stories;
    private readonly IWishlistService _wishlist;
    private readonly IStoreFollowerService _follows;

    public StoreController(
        IStoreService stores,
        IProductService products,
        IStoryService stories,
        IWishlistService wishlist,
        IStoreFollowerService follows)
    {
        _stores = stores;
        _products = products;
        _stories = stories;
        _wishlist = wishlist;
        _follows = follows;
    }

    /// <summary>/Store — the "Shops" tab. Grid of stores with live search.</summary>
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        var result = await _stores.GetListAsync(new StoreListQueryDto
        {
            Page = page,
            PageSize = 24,
            Search = search,
            IsActive = true
        }, ct);

        return View(new ShopsIndexViewModel
        {
            Stores = result,
            Search = search,
            Page = page
        });
    }

    /// <summary>
    /// /Store/Detail/{slug} — full public storefront: cover banner, circle
    /// avatar, name + description, Follow CTA, social chips, Stories rail,
    /// and the store's product grid with sort + in-stock filter.
    /// </summary>
    [Route("Store/Detail/{slug}")]
    public async Task<IActionResult> Detail(string slug, string? sort, bool inStock = false, CancellationToken ct = default)
    {
        StoreDetailDto store;
        try { store = await _stores.GetBySlugAsync(slug, ct); }
        catch (NotFoundException) { return NotFound(); }

        // Sequential awaits — single scoped DbContext, no Task.WhenAll.
        List<ProductListDto> products = await _products.GetByStoreAsync(store.Id, ct);
        if (inStock) products = products.Where(p => p.StockQuantity > 0).ToList();
        products = (sort ?? "newest") switch
        {
            "price-asc" => products.OrderBy(p => p.DiscountPrice ?? p.Price).ToList(),
            "price-desc" => products.OrderByDescending(p => p.DiscountPrice ?? p.Price).ToList(),
            _ => products.OrderByDescending(p => p.CreatedDate).ToList()
        };

        var stories = await _stories.GetByStoreAsync(store.Id, activeOnly: true, ct);
        int followers = await _follows.GetFollowerCountAsync(store.Id, ct);
        bool followed = await _follows.IsFollowedByCurrentUserAsync(store.Id, ct);
        HashSet<int> wishlistIds = await _wishlist.GetMyProductIdsAsync(ct);

        return View(new StoreDetailPageViewModel
        {
            Store = store,
            Products = products,
            Stories = stories,
            FollowerCount = followers,
            IsFollowedByCurrentUser = followed,
            WishlistProductIds = wishlistIds,
            Sort = sort ?? "newest",
            ShowInStockOnly = inStock
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow([FromForm] int storeId, CancellationToken ct)
    {
        try
        {
            FollowResult result = await _follows.FollowAsync(storeId, ct);
            return Json(new { ok = true, isFollowing = result.IsFollowing, followerCount = result.FollowerCount });
        }
        catch (UnauthorizedException ex) { return Unauthorized(new { ok = false, error = ex.Message }); }
        catch (NotFoundException ex)     { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)     { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfollow([FromForm] int storeId, CancellationToken ct)
    {
        try
        {
            FollowResult result = await _follows.UnfollowAsync(storeId, ct);
            return Json(new { ok = true, isFollowing = result.IsFollowing, followerCount = result.FollowerCount });
        }
        catch (UnauthorizedException ex) { return Unauthorized(new { ok = false, error = ex.Message }); }
        catch (NotFoundException ex)     { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)     { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }
}
