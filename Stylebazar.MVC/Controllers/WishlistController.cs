using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Wishlist;
using Stylebazar.MVC.ViewModels;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// MVC face of the wishlist: an authenticated full page at /Wishlist and JSON
/// endpoints used by the heart-toggle on product cards. Auth is enforced at
/// the class level — anon callers get a 401 from <see cref="Toggle"/> and
/// the heart-button JS redirects them to login with a returnUrl.
/// </summary>
[Authorize]
public class WishlistController : Controller
{
    private readonly IWishlistService _wishlist;

    public WishlistController(IWishlistService wishlist)
    {
        _wishlist = wishlist;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await _wishlist.GetMineAsync(ct);
        return View(new WishlistIndexViewModel { Items = items });
    }

    /// <summary>
    /// Idempotent heart toggle used by product cards and the detail page.
    /// Returns the new state + the user's wishlist count so the header
    /// counter and the heart icon can update in one round-trip.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle([FromForm] int productId, CancellationToken ct)
    {
        try
        {
            // Try to remove first — if it was there, this branch wins.
            await _wishlist.RemoveAsync(productId, ct);
            var count = await _wishlist.GetMyCountAsync(ct);
            return Json(new { isInWishlist = false, count });
        }
        catch (NotFoundException)
        {
            // Wasn't in the wishlist → add it.
            try
            {
                await _wishlist.AddAsync(productId, ct);
                var count = await _wishlist.GetMyCountAsync(ct);
                return Json(new { isInWishlist = true, count });
            }
            catch (NotFoundException)
            {
                return NotFound(new { error = "Məhsul tapılmadı." });
            }
            catch (BusinessException ex)
            {
                // Concurrent add — treat as already-in.
                return Json(new { isInWishlist = true, count = await _wishlist.GetMyCountAsync(ct), info = ex.Message });
            }
        }
    }

    /// <summary>
    /// Explicit remove used by the wishlist page's remove buttons (so we don't
    /// accidentally re-add when a user clicks "remove").
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove([FromForm] int productId, CancellationToken ct)
    {
        try
        {
            await _wishlist.RemoveAsync(productId, ct);
            var count = await _wishlist.GetMyCountAsync(ct);
            return Json(new { isInWishlist = false, count });
        }
        catch (NotFoundException)
        {
            return NotFound(new { error = "Wishlist-də belə məhsul yoxdur." });
        }
    }
}
