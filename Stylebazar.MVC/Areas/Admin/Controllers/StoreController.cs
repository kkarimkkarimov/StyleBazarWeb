using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StoreController : Controller
{
    private readonly IStoreService _stores;

    public StoreController(IStoreService stores) { _stores = stores; }

    public async Task<IActionResult> Index(string? search, bool? isActive, int page = 1, CancellationToken ct = default)
    {
        var result = await _stores.GetListAsync(new StoreListQueryDto
        {
            Page = page, PageSize = 25, Search = search, IsActive = isActive
        }, ct);

        return View(new AdminStoresPageViewModel
        {
            Stores = result, Search = search, IsActive = isActive, Page = page
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        try { await _stores.ToggleActiveAsync(id, ct); TempData["Success"] = "Mağaza statusu dəyişdi."; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try { await _stores.DeleteAsync(id, ct); TempData["Success"] = "Mağaza silindi."; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction(int[] ids, string action, CancellationToken ct)
    {
        if (ids is null || ids.Length == 0) { TempData["Error"] = "Heç bir mağaza seçilməyib."; return RedirectToAction(nameof(Index)); }
        int ok = 0, fail = 0;
        foreach (int id in ids)
        {
            try
            {
                if (action == "toggle") await _stores.ToggleActiveAsync(id, ct);
                else if (action == "delete") await _stores.DeleteAsync(id, ct);
                ok++;
            }
            catch { fail++; }
        }
        TempData[fail == 0 ? "Success" : "Error"] = $"{ok} icra olundu, {fail} uğursuz.";
        return RedirectToAction(nameof(Index));
    }
}
