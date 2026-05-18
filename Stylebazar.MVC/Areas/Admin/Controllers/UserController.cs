using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Account;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly IAccountService _account;
    private readonly IStoreService _stores;

    public UserController(IAccountService account, IStoreService stores)
    {
        _account = account;
        _stores = stores;
    }

    public async Task<IActionResult> Index(UserAdminListFilter filter, CancellationToken ct)
    {
        var page = await _account.GetUsersAsync(filter, ct);
        return View(new AdminUsersPageViewModel { Users = page, Filter = filter });
    }

    public async Task<IActionResult> Detail(string id, CancellationToken ct)
    {
        var user = await _account.GetUserByIdAsync(id, ct);
        if (user is null) { TempData["Error"] = "İstifadəçi tapılmadı."; return RedirectToAction(nameof(Index)); }

        List<StoreListDto> stores = (await _stores.GetListAsync(
            new StoreListQueryDto { Page = 1, PageSize = 50, OwnerId = id }, ct)).Items.ToList();

        return View(new AdminUserDetailViewModel
        {
            User = user,
            Stores = stores
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role, CancellationToken ct)
    {
        try { await _account.SetRoleAsync(id, role, ct); TempData["Success"] = "Rol yeniləndi."; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSuspension(string id, string? returnTo, CancellationToken ct)
    {
        try
        {
            bool suspended = await _account.ToggleSuspensionAsync(id, ct);
            TempData["Success"] = suspended ? "İstifadəçi blok edildi." : "İstifadəçinin bloku açıldı.";
        }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return returnTo == "list" ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        try { await _account.DeleteUserAsync(id, ct); TempData["Success"] = "İstifadəçi silindi."; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction(string[] ids, string action, CancellationToken ct)
    {
        if (ids is null || ids.Length == 0) { TempData["Error"] = "Heç bir istifadəçi seçilməyib."; return RedirectToAction(nameof(Index)); }
        int ok = 0, fail = 0;
        foreach (string id in ids)
        {
            try
            {
                if (action == "suspend") await _account.ToggleSuspensionAsync(id, ct);
                else if (action == "delete") await _account.DeleteUserAsync(id, ct);
                ok++;
            }
            catch { fail++; }
        }
        TempData[fail == 0 ? "Success" : "Error"] = $"{ok} icra olundu, {fail} uğursuz.";
        return RedirectToAction(nameof(Index));
    }
}
