using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.DTOs.Subscriptions;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Account;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.BLL.Services.Subscriptions;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.MVC.Areas.Admin.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly IAccountService _account;
    private readonly IStoreService _stores;
    private readonly ISubscriptionService _subscriptions;

    public UserController(IAccountService account, IStoreService stores, ISubscriptionService subscriptions)
    {
        _account = account;
        _stores = stores;
        _subscriptions = subscriptions;
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

        var currentSub = await _subscriptions.GetCurrentForUserAsync(id, ct);
        var history = await _subscriptions.GetHistoryAsync(id, take: 10, ct);

        return View(new AdminUserDetailViewModel
        {
            User = user,
            Stores = stores,
            CurrentSubscription = currentSub,
            SubscriptionHistory = history
        });
    }

    /// <summary>
    /// Admin upgrades or downgrades a user's plan after manual payment
    /// review. Writes a Subscription audit row + flips AppUser cached
    /// fields atomically (see SubscriptionManager.ChangePlanAsync).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeSubscription(
        string id,
        SubscriptionPlan plan,
        DateTime? expiresAt,
        string? notes,
        CancellationToken ct)
    {
        try
        {
            string adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await _subscriptions.ChangePlanAsync(new UpgradeSubscriptionDto
            {
                UserId = id,
                Plan = plan,
                ExpiresAt = expiresAt,
                Notes = notes
            }, adminId, ct);

            TempData["Success"] = plan == SubscriptionPlan.Free
                ? "Plan Free-yə endirildi."
                : $"{plan} planı aktivləşdirildi.";
        }
        catch (ValidationException ex)
        {
            // FluentValidation failures from UpgradeSubscriptionValidator
            // (e.g. past expiry, missing UserId) — surface the first error.
            TempData["Error"] = ex.Errors.Values.SelectMany(v => v).FirstOrDefault() ?? "Form məlumatı etibarsızdır.";
        }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex)  { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
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
