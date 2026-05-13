using System.Diagnostics;
using Core.Persistence.Paging;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Newsletter;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Categories;
using Stylebazar.BLL.Services.HeroBanners;
using Stylebazar.BLL.Services.Newsletter;
using Stylebazar.BLL.Services.Products;
using Stylebazar.BLL.Services.Stories;
using Stylebazar.BLL.Services.Wishlist;
using Stylebazar.MVC.Models;
using Stylebazar.MVC.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IHeroBannerService _banners;
    private readonly IStoryService _stories;
    private readonly INewsletterService _newsletter;
    private readonly IWishlistService _wishlist;

    public HomeController(
        IProductService products,
        ICategoryService categories,
        IHeroBannerService banners,
        IStoryService stories,
        INewsletterService newsletter,
        IWishlistService wishlist)
    {
        _products = products;
        _categories = categories;
        _banners = banners;
        _stories = stories;
        _newsletter = newsletter;
        _wishlist = wishlist;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Sequential awaits — one scoped DbContext per request, so we cannot
        // fire these in parallel with Task.WhenAll without crashing EF Core.
        var banners = await _banners.GetActiveAsync(ct);
        var stories = await _stories.GetActiveFeedAsync(ct);
        var tree = await _categories.GetTreeAsync(ct);
        var featured = await _products.GetFeaturedAsync(8, ct);
        Paginate<ProductListDto> latest = await _products.GetListAsync(
            new ProductListQueryDto { Page = 1, PageSize = 8, IsActive = true },
            ct);

        // Wishlist IDs — empty set for anon. Used to paint heart icons on
        // every product card without one round-trip per card.
        HashSet<int> wishlistIds = await _wishlist.GetMyProductIdsAsync(ct);

        var vm = new HomeIndexViewModel
        {
            Banners = banners,
            Stories = stories,
            Categories = tree.Take(8).ToList(),
            FeaturedProducts = featured,
            LatestProducts = latest.Items.ToList(),
            WishlistProductIds = wishlistIds
        };

        return View(vm);
    }

    public IActionResult Privacy() => View();

    /// <summary>
    /// AJAX endpoint used by the homepage newsletter form. Anonymous, idempotent.
    /// </summary>
    [HttpPost]
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
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
