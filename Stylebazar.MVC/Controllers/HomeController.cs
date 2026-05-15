using System.Diagnostics;
using Core.Persistence.Paging;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Newsletter;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Categories;
using Stylebazar.BLL.Services.Newsletter;
using Stylebazar.BLL.Services.Products;
using Stylebazar.BLL.Services.Wishlist;
using Stylebazar.MVC.Models;
using Stylebazar.MVC.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// "Ana səhifə" — the root URL "/" now serves the product catalog
/// (the same page that used to live under "Mağaza"/"/Shop"). We re-use
/// the existing Shop/Index view so there is a single source of truth
/// for the catalog UI; /Shop remains as a legacy alias.
/// </summary>
public class HomeController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly INewsletterService _newsletter;
    private readonly IWishlistService _wishlist;

    public HomeController(
        IProductService products,
        ICategoryService categories,
        INewsletterService newsletter,
        IWishlistService wishlist)
    {
        _products = products;
        _categories = categories;
        _newsletter = newsletter;
        _wishlist = wishlist;
    }

    public async Task<IActionResult> Index([FromQuery] ProductListQueryDto query, CancellationToken ct)
    {
        // Mirror ShopController.Index — buyers see only active products,
        // sellers and admins can flip the IsActive flag via query string.
        if (!User.IsInRole("Admin") && !User.IsInRole("Seller") && !query.IsActive.HasValue)
            query.IsActive = true;

        Paginate<ProductListDto> products = await _products.GetListAsync(query, ct);
        Paginate<CategoryListDto> catPage = await _categories.GetListAsync(
            new CategoryListQueryDto { Page = 1, PageSize = 100 }, ct);
        HashSet<int> wishlistIds = await _wishlist.GetMyProductIdsAsync(ct);

        var vm = new ShopIndexViewModel
        {
            Query = query,
            Products = products,
            Categories = catPage.Items.ToList(),
            Brands = products.Items
                .Where(p => !string.IsNullOrWhiteSpace(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToList(),
            WishlistProductIds = wishlistIds
        };

        // Re-use the catalog view. "Ana səhifə" is just a renamed entry
        // point — the page content is the catalog the user expects.
        return View("~/Views/Shop/Index.cshtml", vm);
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
