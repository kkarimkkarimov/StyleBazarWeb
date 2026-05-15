using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Account;
using Stylebazar.BLL.Services.Addresses;
using Stylebazar.BLL.Services.Authentication;
using Stylebazar.BLL.Services.LoginEvents;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.MVC.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IAccountService _account;
    private readonly IAddressService _addresses;
    private readonly ILoginEventService _events;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public AccountController(
        IAuthService authService,
        IAccountService account,
        IAddressService addresses,
        ILoginEventService events,
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager)
    {
        _authService = authService;
        _account = account;
        _addresses = addresses;
        _events = events;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // -------- Login --------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken ct = default)
    {
        ViewData["ReturnUrl"] = returnUrl;

        // Defensive: if a stale form is POSTed by an already-signed-in tab,
        // just send them home — don't double-log a fresh sign-in.
        if (_signInManager.IsSignedIn(User))
            return SafeLocalRedirect(returnUrl);

        if (!ModelState.IsValid)
            return View(model);

        AppUser? user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            // Same message whether user is missing or password is wrong — no
            // account-enumeration via the response.
            ModelState.AddModelError(string.Empty, "Email və ya parol səhvdir.");
            return View(model);
        }

        Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            await _events.TrackAsync(user.Id, LoginEventType.FailedSignIn,
                GetClientIp(), GetUserAgent(), ct);
            ModelState.AddModelError(string.Empty, "Email və ya parol səhvdir.");
            return View(model);
        }

        await _events.TrackAsync(user.Id, LoginEventType.SignIn,
            GetClientIp(), GetUserAgent(), ct);

        return SafeLocalRedirect(returnUrl);
    }

    // -------- Register --------

    [HttpGet]
    public IActionResult Register()
    {
        if (_signInManager.IsSignedIn(User))
            return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct = default)
    {
        if (_signInManager.IsSignedIn(User))
            return RedirectToAction("Index", "Home");

        if (!ModelState.IsValid)
            return View(model);

        RegisterDto dto = new()
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword,
            Role = model.Role
        };

        RegisterResultDto result = await _authService.RegisterAsync(dto, ct);

        if (!result.Succeeded)
        {
            // Field-keyed errors land next to their inputs.
            foreach ((string field, string[] messages) in result.FieldErrors)
                foreach (string msg in messages)
                    ModelState.AddModelError(field, msg);

            // Anything that doesn't map to a field shows in the form summary.
            if (!string.IsNullOrEmpty(result.GeneralError))
                ModelState.AddModelError(string.Empty, result.GeneralError);

            return View(model);
        }

        // Successful create → sign in + audit log + welcome toast.
        AppUser? created = await _userManager.FindByIdAsync(result.UserId!);
        if (created is not null)
        {
            await _signInManager.SignInAsync(created, isPersistent: false);
            await _events.TrackAsync(created.Id, LoginEventType.SignUp,
                GetClientIp(), GetUserAgent(), ct);
        }

        TempData["Success"] = $"Xoş gəldin, {result.FirstName}! Qeydiyyatın tamamlandı.";
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Safely follows a returnUrl when local, otherwise sends to home.
    /// Centralizes the open-redirect guard so every signed-in entry point
    /// behaves the same way.
    /// </summary>
    private IActionResult SafeLocalRedirect(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    // -------- Logout --------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct = default)
    {
        string? userId = _userManager.GetUserId(User);
        if (!string.IsNullOrEmpty(userId))
        {
            await _events.TrackAsync(userId, LoginEventType.SignOut,
                GetClientIp(), GetUserAgent(), ct);
        }

        await _signInManager.SignOutAsync();
        TempData["Success"] = "Sistemdən çıxdın. Yenidən görüşənədək!";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // =========================================================
    // ============== ACCOUNT DASHBOARD (image 1) ==============
    // =========================================================

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var profile = await _account.GetMineAsync(ct);
        return View(new AccountPanelViewModel { Profile = profile });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var profile = await _account.GetMineAsync(ct);
        var sessions = await _events.GetMyActiveSessionsAsync(ct);
        var history = await _events.GetMyHistoryAsync(30, ct);

        return View(new AccountProfilePageViewModel
        {
            Profile = profile,
            ActiveSessions = sessions,
            LoginHistory = history
        });
    }

    [HttpPost]
    [Authorize]
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
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(
        [FromForm] string currentPassword,
        [FromForm] string newPassword,
        [FromForm] string confirmNewPassword,
        CancellationToken ct)
    {
        try
        {
            var dto = new ChangePasswordDto
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword,
                ConfirmNewPassword = confirmNewPassword
            };
            await _account.ChangePasswordAsync(dto, ct);

            // Best-effort audit log
            string? userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
                await _events.TrackAsync(userId, LoginEventType.PasswordChanged,
                    GetClientIp(), GetUserAgent(), ct);

            return Json(new { ok = true });
        }
        catch (ValidationException ex) { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { ok = false, error = "Şəkil seçilməyib." });

        try
        {
            string url = await _account.UploadAvatarAsync(file, ct);
            return Json(new { ok = true, url });
        }
        catch (ValidationException ex) { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    // Placeholder stubs for sidebar nav items (Phase 6b+)

    [HttpGet] [Authorize]
    public async Task<IActionResult> Notifications(CancellationToken ct)
    {
        return View(new AccountPanelViewModel { Profile = await _account.GetMineAsync(ct) });
    }

    [HttpGet] [Authorize]
    public async Task<IActionResult> Messages(CancellationToken ct)
    {
        return View(new AccountPanelViewModel { Profile = await _account.GetMineAsync(ct) });
    }

    [HttpGet] [Authorize]
    public async Task<IActionResult> Addresses(CancellationToken ct)
    {
        var profile = await _account.GetMineAsync(ct);
        var list = await _addresses.GetMineAsync(ct);
        return View(new AccountAddressesPageViewModel
        {
            Profile = profile,
            Addresses = list
        });
    }

    // Wishlist moved to its own controller.
    [HttpGet]
    public IActionResult Wishlist() => RedirectToActionPermanent("Index", "Wishlist");

    // -------- helpers --------

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() =>
        HttpContext.Request.Headers["User-Agent"].ToString();
}
