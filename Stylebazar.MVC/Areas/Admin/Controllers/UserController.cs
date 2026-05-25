using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.DTOs.Subscriptions;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Account;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.BLL.Services.Subscriptions;
using Stylebazar.DAL.Entities;
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
    private readonly UserManager<AppUser> _users;

    public UserController(IAccountService account, IStoreService stores, ISubscriptionService subscriptions, UserManager<AppUser> users)
    {
        _account = account;
        _stores = stores;
        _subscriptions = subscriptions;
        _users = users;
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
        // Self-demote guard: if an admin demotes themselves and is the last admin,
        // the panel becomes unreachable. Simpler rule: forbid changing your own role
        // entirely — admins can be promoted/demoted by another admin (or via the
        // Admin:Email seed in DEPLOYMENT.md).
        if (await IsSelfAsync(id))
        {
            TempData["Error"] = "Öz rolunu dəyişə bilməzsən. Başqa adminə müraciət et.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        try { await _account.SetRoleAsync(id, role, ct); TempData["Success"] = "Rol yeniləndi."; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSuspension(string id, string? returnTo, CancellationToken ct)
    {
        if (await IsSelfAsync(id))
        {
            TempData["Error"] = "Özünü blok edə bilməzsən.";
            return returnTo == "list" ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Detail), new { id });
        }
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
        if (await IsSelfAsync(id))
        {
            TempData["Error"] = "Öz hesabını silə bilməzsən.";
            return RedirectToAction(nameof(Detail), new { id });
        }
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
        // Filter out self silently from bulk — keeps the action safe even if the
        // admin includes their own checkbox by mistake.
        AppUser? meUser = await _users.GetUserAsync(User);
        string me = meUser?.Id ?? string.Empty;
        string[] targets = ids.Where(i => !string.Equals(i, me, StringComparison.OrdinalIgnoreCase)).ToArray();
        int skippedSelf = ids.Length - targets.Length;

        int ok = 0;
        var failures = new List<(string Id, string Reason)>();
        foreach (string id in targets)
        {
            try
            {
                if (action == "suspend") await _account.ToggleSuspensionAsync(id, ct);
                else if (action == "delete") await _account.DeleteUserAsync(id, ct);
                ok++;
            }
            catch (NotFoundException ex) { failures.Add((id, ex.Message)); }
            catch (BusinessException ex) { failures.Add((id, ex.Message)); }
            catch (Exception ex) { failures.Add((id, "Sistem xətası: " + ex.GetType().Name)); }
        }
        string selfMsg = skippedSelf > 0 ? " (öz hesabın atlandı)" : string.Empty;
        if (failures.Count == 0)
        {
            TempData["Success"] = $"{ok} istifadəçi icra olundu{selfMsg}.";
        }
        else
        {
            var grouped = failures.GroupBy(f => f.Reason).Select(g => $"{g.Count()}× {g.Key}");
            TempData["Error"] = $"{ok} icra olundu, {failures.Count} uğursuz{selfMsg}: " + string.Join(" • ", grouped);
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Async self-check: resolves the current user via <c>UserManager.GetUserAsync</c>
    /// (the gold-standard Identity helper — works even if the cookie's claim layout
    /// is non-default) and compares the resolved Id against the target. The synchronous
    /// <c>GetUserId(User)</c> route via raw claim lookup returned null intermittently
    /// in this app, so we round-trip through the user store to be safe.
    /// </summary>
    /// <summary>
    /// Async self-check: round-trips through <c>UserManager.GetUserAsync</c>
    /// instead of reading <c>ClaimTypes.NameIdentifier</c> directly because
    /// that claim was empirically unreliable from the controller context in
    /// this app (the layout's same call worked, the controller's didn't).
    /// </summary>
    private async Task<bool> IsSelfAsync(string targetUserId)
    {
        AppUser? me = await _users.GetUserAsync(User);
        return me?.Id is { } meId && string.Equals(meId, targetUserId, StringComparison.OrdinalIgnoreCase);
    }
}
