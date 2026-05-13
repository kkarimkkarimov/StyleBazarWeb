using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Reviews;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Categories;
using Stylebazar.BLL.Services.Products;
using Stylebazar.BLL.Services.Reviews;
using Stylebazar.MVC.ViewModels;

namespace Stylebazar.MVC.Controllers;

public class ShopController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IReviewService _reviews;

    public ShopController(
        IProductService products,
        ICategoryService categories,
        IReviewService reviews)
    {
        _products = products;
        _categories = categories;
        _reviews = reviews;
    }

    public async Task<IActionResult> Index([FromQuery] ProductListQueryDto query, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Seller") && !query.IsActive.HasValue)
            query.IsActive = true;

        var products = await _products.GetListAsync(query, ct);
        var catPage = await _categories.GetListAsync(new CategoryListQueryDto { Page = 1, PageSize = 100 }, ct);

        return View(new ShopIndexViewModel
        {
            Query = query,
            Products = products,
            Categories = catPage.Items.ToList(),
            Brands = products.Items
                .Where(p => !string.IsNullOrWhiteSpace(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToList()
        });
    }

    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        try
        {
            var product = await _products.GetByIdAsync(id, ct);
            var reviews = await _reviews.GetByProductAsync(id,
                new ReviewListQueryDto { Page = 1, PageSize = 10 }, ct);

            // "Daha çox mağazadan" — same store products, excluding current.
            var related = await _products.GetByStoreAsync(product.StoreId, ct);
            related = related.Where(p => p.Id != id).Take(4).ToList();

            return View(new ShopDetailViewModel
            {
                Product = product,
                Reviews = reviews,
                RelatedFromStore = related
            });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
