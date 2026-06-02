using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Categories;
using Stylebazar.BLL.Services.Products;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

// ProductController (Admin paneli) — yalnız adminə açıq əməliyyatlar üçün controller. İstəyi alır, biznes xidmətini çağırır, idarəetmə nəticəsini/səhifəsini qaytarır.
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IStoreService _stores;

    public ProductController(IProductService products, ICategoryService categories, IStoreService stores)
    {
        _products = products;
        _categories = categories;
        _stores = stores;
    }

    public async Task<IActionResult> Index(ProductListQueryDto query, CancellationToken ct)
    {
        query.Page = Math.Max(query.Page, 1);
        query.PageSize = query.PageSize > 0 && query.PageSize <= 100 ? query.PageSize : 25;

        var page = await _products.GetListAsync(query, ct);
        var cats = await _categories.GetListAsync(new CategoryListQueryDto { Page = 1, PageSize = 200 }, ct);
        var stores = await _stores.GetListAsync(new StoreListQueryDto { Page = 1, PageSize = 200 }, ct);

        return View(new AdminProductsPageViewModel
        {
            Products = page,
            Categories = cats.Items.ToList(),
            Stores = stores.Items.ToList(),
            Query = query
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        try { await _products.ToggleActiveAsync(id, ct); TempData["Success"] = "Məhsul statusu dəyişdi."; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(int id, int? days, CancellationToken ct)
    {
        try
        {
            // Real toggle: if the product is already featured, clear; otherwise
            // apply (with `days` honored — view passes 14 by default).
            var current = await _products.GetByIdAsync(id, ct);
            bool active = current.IsFeatured && (current.FeaturedUntil == null || current.FeaturedUntil > DateTime.UtcNow);
            if (active)
            {
                await _products.ClearFeaturedAsync(id, ct);
                TempData["Success"] = "Featured ləğv edildi.";
            }
            else
            {
                await _products.SetFeaturedAsync(id, days, ct);
                TempData["Success"] = days.HasValue
                    ? $"Məhsul {days} gün üçün featured edildi."
                    : "Məhsul featured edildi.";
            }
        }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try { await _products.DeleteAsync(id, ct); TempData["Success"] = "Məhsul silindi."; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction(int[] ids, string action, CancellationToken ct)
    {
        if (ids is null || ids.Length == 0) { TempData["Error"] = "Heç bir məhsul seçilməyib."; return RedirectToAction(nameof(Index)); }
        int ok = 0;
        var failures = new List<(int Id, string Reason)>();
        foreach (int id in ids)
        {
            try
            {
                if (action == "toggle") await _products.ToggleActiveAsync(id, ct);
                else if (action == "feature") await _products.SetFeaturedAsync(id, 14, ct);
                else if (action == "delete") await _products.DeleteAsync(id, ct);
                ok++;
            }
            catch (NotFoundException ex) { failures.Add((id, ex.Message)); }
            catch (BusinessException ex) { failures.Add((id, ex.Message)); }
            catch (Exception ex) { failures.Add((id, "Sistem xətası: " + ex.GetType().Name)); }
        }
        if (failures.Count == 0)
        {
            TempData["Success"] = $"{ok} məhsul icra olundu.";
        }
        else
        {
            var grouped = failures.GroupBy(f => f.Reason).Select(g => $"{g.Count()}× {g.Key}");
            TempData["Error"] = $"{ok} icra olundu, {failures.Count} uğursuz: " + string.Join(" • ", grouped);
        }
        return RedirectToAction(nameof(Index));
    }
}
