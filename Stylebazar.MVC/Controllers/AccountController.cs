using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Authentication;
using Stylebazar.DAL.Entities;
using Stylebazar.MVC.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public AccountController(
        IAuthService authService,
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager)
    {
        _authService = authService;
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
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        AppUser? user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Email və ya parol səhvdir.");
            return View(model);
        }

        Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Email və ya parol səhvdir.");
            return View(model);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
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
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            RegisterDto dto = new()
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                Role = model.Role
            };

            IdentityResult result = await _authService.RegisterAsync(dto);
            if (!result.Succeeded)
            {
                foreach (IdentityError err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);
                return View(model);
            }

            // Auto sign-in (cookie) so the user lands on home as authenticated
            AppUser? created = await _userManager.FindByEmailAsync(model.Email);
            if (created is not null)
                await _signInManager.SignInAsync(created, isPersistent: false);

            TempData["Success"] = $"Xoş gəldin, {model.FullName.Split(' ')[0]}! Qeydiyyatın tamamlandı.";
            return RedirectToAction("Index", "Home");
        }
        catch (ValidationException vex)
        {
            foreach (KeyValuePair<string, string[]> kv in vex.Errors)
                foreach (string msg in kv.Value)
                    ModelState.AddModelError(kv.Key, msg);
            return View(model);
        }
        catch (BusinessException bex)
        {
            ModelState.AddModelError(string.Empty, bex.Message);
            return View(model);
        }
    }

    // -------- Logout --------

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "Sistemdən çıxdın. Yenidən görüşənədək!";
        return RedirectToAction("Index", "Home");
    }

    // -------- AccessDenied --------

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // -------- Profile (placeholder until Phase 4) --------

    [HttpGet]
    [Authorize]
    public IActionResult Profile() => View();

    [HttpGet]
    [Authorize]
    public IActionResult Wishlist() => View();
}
