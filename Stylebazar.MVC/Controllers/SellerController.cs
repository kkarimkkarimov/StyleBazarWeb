using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Account;
using Stylebazar.BLL.Services.LoginEvents;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.MVC.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// "Seller Panel" (image 2) — vendor-scoped pages under /Seller/*.
/// Auth-gated to Seller (or Admin override).
/// </summary>
[Authorize(Roles = "Seller,Admin")]
public class SellerController : Controller
{
    private readonly IAccountService _account;
    private readonly IStoreService _stores;
    private readonly ILoginEventService _events;
    private readonly UserManager<AppUser> _userManager;

    public SellerController(
        IAccountService account,
        IStoreService stores,
        ILoginEventService events,
        UserManager<AppUser> userManager)
    {
        _account = account;
        _stores = stores;
        _events = events;
        _userManager = userManager;
    }

    // Dashboard — overview placeholder for Phase 6c (stats + recent activity).
    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        return View(await BuildPanelVmAsync(ct));
    }

    // ============== PROFILE (image 2) ==============

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var vm = await BuildPanelVmAsync(ct);
        var sessions = await _events.GetMyActiveSessionsAsync(ct);
        var history = await _events.GetMyHistoryAsync(30, ct);

        return View(new SellerProfilePageViewModel
        {
            Profile = vm.Profile,
            PrimaryStore = vm.PrimaryStore,
            ActiveSessions = sessions,
            LoginHistory = history
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        [FromForm] string fullName,
        [FromForm] string? phoneNumber,
        [FromForm] string? bio,
        CancellationToken ct)
    {
        try
        {
            var dto = new UpdateProfileDto { FullName = fullName, PhoneNumber = phoneNumber, Bio = bio };
            var updated = await _account.UpdateProfileAsync(dto, ct);
            return Json(new { ok = true, profile = updated });
        }
        catch (ValidationException ex) { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStoreContacts(
        [FromForm] int storeId,
        [FromForm] string? whatsAppNumber,
        [FromForm] string? instagramHandle,
        [FromForm] string? telegramHandle,
        [FromForm] string? facebookHandle,
        [FromForm] string? tikTokHandle,
        CancellationToken ct)
    {
        try
        {
            var existing = await _stores.GetByIdAsync(storeId, ct);
            var update = new UpdateStoreDto
            {
                Name = existing.Name,
                Slug = existing.Slug,
                Description = existing.Description,
                ProfileImageUrl = existing.ProfileImageUrl,
                CoverImageUrl = existing.CoverImageUrl,
                ContactPhone = existing.ContactPhone,
                Address = existing.Address,
                WhatsAppNumber = whatsAppNumber,
                InstagramHandle = instagramHandle,
                TelegramHandle = telegramHandle,
                FacebookHandle = facebookHandle,
                TikTokHandle = tikTokHandle
            };
            await _stores.UpdateAsync(storeId, update, ct);
            return Json(new { ok = true });
        }
        catch (NotFoundException) { return NotFound(); }
        catch (BusinessException ex) { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    // ============== STUB PAGES (Phase 6b+ will flesh out) ==============

    [HttpGet] public async Task<IActionResult> Products(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> AddProduct(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> SmartUpload(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Reviews(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Notifications(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Messages(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Packages(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Integration(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Analytics(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> StoreSettings(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    // -------- helpers --------

    private async Task<SellerPanelViewModel> BuildPanelVmAsync(CancellationToken ct)
    {
        var profile = await _account.GetMineAsync(ct);
        var stores = await _stores.GetMyStoresAsync(ct);
        StoreDetailDto? primary = null;
        if (stores.Any())
        {
            try { primary = await _stores.GetByIdAsync(stores.First().Id, ct); }
            catch { /* swallow — primary is optional */ }
        }
        return new SellerPanelViewModel { Profile = profile, PrimaryStore = primary };
    }
}
