using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.Services.Categories;
using Stylebazar.BLL.Services.Products;
using Stylebazar.MVC.ViewModels;

namespace Stylebazar.MVC.Controllers;

public class ShopController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;

    public ShopController(IProductService products, ICategoryService categories)
    {
        _products = products;
        _categories = categories;
    }

    public async Task<IActionResult> Index([FromQuery] ProductListQueryDto query, CancellationToken ct)
    {
        // Public visitors only see active products unless they ask otherwise.
        if (!User.IsInRole("Admin") && !User.IsInRole("Seller") && !query.IsActive.HasValue)
            query.IsActive = true;

        var products = await _products.GetListAsync(query, ct);

        // Categories sidebar — flat list of all categories ordered by name.
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

    public IActionResult Detail(int id) => RedirectToAction(nameof(Index));
}
