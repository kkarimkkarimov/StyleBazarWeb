using System.Diagnostics;
using Core.Persistence.Paging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Stylebazar.BLL.DTOs.Newsletter;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Newsletter;
using Stylebazar.BLL.Services.Products;
using Stylebazar.BLL.Services.Stories;
using Stylebazar.BLL.Services.Wishlist;
using Stylebazar.MVC.Models;
using Stylebazar.MVC.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// "Ana səhifə" — Geyin-style tile landing: Featured rail + Stories rail
/// + Latest products grid. The detailed catalog with filter sidebar
/// lives at /Shop ("Mağaza"), reachable from category mega-menu and
/// "Hamısına bax" links on each tile.
/// </summary>
public class HomeController : Controller
{
    private readonly IProductService _products;
    private readonly IStoryService _stories;
    private readonly INewsletterService _newsletter;
    private readonly IWishlistService _wishlist;

    public HomeController(
        IProductService products,
        IStoryService stories,
        INewsletterService newsletter,
        IWishlistService wishlist)
    {
        _products = products;
        _stories = stories;
        _newsletter = newsletter;
        _wishlist = wishlist;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Sequential awaits — scoped DbContext, no Task.WhenAll.
        var stories = await _stories.GetActiveFeedAsync(ct);
        var featured = await _products.GetFeaturedAsync(12, ct);
        Paginate<ProductListDto> latest = await _products.GetListAsync(
            new ProductListQueryDto { Page = 1, PageSize = 18, IsActive = true },
            ct);

        HashSet<int> wishlistIds = await _wishlist.GetMyProductIdsAsync(ct);

        return View(new HomeIndexViewModel
        {
            Stories = stories,
            FeaturedProducts = featured,
            LatestProducts = latest.Items.ToList(),
            WishlistProductIds = wishlistIds
        });
    }

    public IActionResult Privacy() => View();

    /// <summary>AJAX endpoint used by the homepage newsletter form.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _newsletter.SubscribeAsync(dto, ct);
            return Ok(new { email = result.Email });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? code = null)
    {
        // The status-code re-execution middleware forwards the original code so
        // the view can show a friendly headline for 404 vs 500.
        ViewData["StatusCode"] = code;
        Response.StatusCode = code ?? 500;
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
