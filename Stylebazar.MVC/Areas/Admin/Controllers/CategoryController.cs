using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Categories;
using Stylebazar.MVC.Areas.Admin.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly ICategoryService _categories;

    public CategoryController(ICategoryService categories) { _categories = categories; }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var all = await _categories.GetListAsync(new CategoryListQueryDto { Page = 1, PageSize = 500 }, ct);
        return View(new AdminCategoriesPageViewModel { Categories = all.Items.ToList() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] string name, [FromForm] string? slug, [FromForm] int? parentCategoryId, CancellationToken ct)
    {
        try
        {
            await _categories.CreateAsync(new CreateCategoryDto
            {
                Name = name?.Trim() ?? string.Empty,
                Slug = slug?.Trim(),
                ParentCategoryId = parentCategoryId
            }, ct);
            TempData["Success"] = "Kateqoriya yaradıldı.";
        }
        catch (ValidationException vex) { TempData["Error"] = string.Join(" • ", vex.Errors.SelectMany(kv => kv.Value)); }
        catch (BusinessException bex) { TempData["Error"] = bex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [FromForm] string name, [FromForm] string? slug, [FromForm] int? parentCategoryId, CancellationToken ct)
    {
        try
        {
            await _categories.UpdateAsync(id, new UpdateCategoryDto
            {
                Name = name?.Trim() ?? string.Empty,
                Slug = slug?.Trim(),
                ParentCategoryId = parentCategoryId
            }, ct);
            TempData["Success"] = "Kateqoriya yeniləndi.";
        }
        catch (ValidationException vex) { TempData["Error"] = string.Join(" • ", vex.Errors.SelectMany(kv => kv.Value)); }
        catch (NotFoundException nex) { TempData["Error"] = nex.Message; }
        catch (BusinessException bex) { TempData["Error"] = bex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try { await _categories.DeleteAsync(id, ct); TempData["Success"] = "Kateqoriya silindi."; }
        catch (NotFoundException nex) { TempData["Error"] = nex.Message; }
        catch (BusinessException bex) { TempData["Error"] = bex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
