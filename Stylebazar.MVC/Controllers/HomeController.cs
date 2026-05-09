using System.Diagnostics;
using Core.Persistence.Paging;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.Services.Categories;
using Stylebazar.BLL.Services.Products;
using Stylebazar.MVC.Models;
using Stylebazar.MVC.ViewModels;

namespace Stylebazar.MVC.Controllers;

public class HomeController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;

    public HomeController(IProductService products, ICategoryService categories)
    {
        _products = products;
        _categories = categories;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Sequential awaits — one scoped DbContext per request, so we cannot
        // fire these in parallel with Task.WhenAll without crashing EF Core.
        var tree = await _categories.GetTreeAsync(ct);
        var featured = await _products.GetFeaturedAsync(8, ct);
        Paginate<ProductListDto> latest = await _products.GetListAsync(
            new ProductListQueryDto { Page = 1, PageSize = 8, IsActive = true },
            ct);

        var vm = new HomeIndexViewModel
        {
            Categories = tree.Take(8).ToList(),
            FeaturedProducts = featured,
            LatestProducts = latest.Items.ToList()
        };

        return View(vm);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
