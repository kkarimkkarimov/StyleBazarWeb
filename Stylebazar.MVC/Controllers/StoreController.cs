using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Stylebazar.BLL.DTOs.Analytics;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Analytics;
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
    private readonly IContactClickService _contactClicks;

    public StoreController(
        IStoreService stores,
        IProductService products,
        IStoryService stories,
        IWishlistService wishlist,
        IStoreFollowerService follows,
        IContactClickService contactClicks)
    {
        _stores = stores;
        _products = products;
        _stories = stories;
        _wishlist = wishlist;
        _follows = follows;
        _contactClicks = contactClicks;
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

    /// <summary>
    /// Fire-and-forget telemetry endpoint. The browser POSTs here right before
    /// it navigates to the WhatsApp / Instagram / phone deep link so we can
    /// capture a row of analytics for the seller dashboard.
    ///
    /// Anonymous OK — anyone can click a contact button. No anti-forgery
    /// because (a) the JS attempts navigation even if this call fails and
    /// (b) the endpoint is pure telemetry (no state change to the caller).
    /// Rate-limited via "write-default" (30 / minute / IP) to keep abuse
    /// from polluting analytics.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> ClickContact([FromBody] RecordContactClickDto dto, CancellationToken ct)
    {
        if (dto is null || dto.StoreId <= 0 || !Enum.IsDefined(typeof(DAL.Entities.Enums.ContactType), dto.ContactType))
        {
            return BadRequest();
        }

        string? userId = User?.Identity?.IsAuthenticated == true
            ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : null;

        string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        string? ua = Request.Headers.UserAgent.ToString();
        string? referer = Request.Headers.Referer.ToString();

        await _contactClicks.RecordAsync(dto, userId, ip, ua, referer, ct);
        return NoContent();
    }
}
