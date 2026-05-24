using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.HeroBanners;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.FileStorage;
using Stylebazar.BLL.Services.HeroBanners;
using Stylebazar.MVC.Areas.Admin.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HeroBannerController : Controller
{
    private const string Subfolder = "hero-banners";

    private readonly IHeroBannerService _service;
    private readonly IFileStorageService _files;

    public HeroBannerController(IHeroBannerService service, IFileStorageService files)
    {
        _service = service;
        _files = files;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var all = await _service.GetAllAsync(ct);
        return View(new AdminHeroBannersPageViewModel { Banners = all });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        [FromForm] string title,
        [FromForm] string? subtitle,
        [FromForm] string? eyebrow,
        [FromForm] string? ctaText,
        [FromForm] string? ctaUrl,
        [FromForm] int displayOrder,
        IFormFile? file,
        CancellationToken ct)
    {
        try
        {
            string? url = null;
            if (file is not null && file.Length > 0)
                url = await _files.SaveAsync(file, Subfolder, ct);

            await _service.CreateAsync(new CreateHeroBannerDto
            {
                Title = title?.Trim() ?? string.Empty,
                Subtitle = subtitle?.Trim(),
                Eyebrow = eyebrow?.Trim(),
                ImageUrl = url,
                CtaText = ctaText?.Trim(),
                CtaUrl = ctaUrl?.Trim(),
                DisplayOrder = displayOrder
            }, ct);
            TempData["Success"] = "Banner yaradıldı.";
        }
        catch (ValidationException vex) { TempData["Error"] = string.Join(" • ", vex.Errors.SelectMany(kv => kv.Value)); }
        catch (BusinessException bex) { TempData["Error"] = bex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] string title,
        [FromForm] string? subtitle,
        [FromForm] string? eyebrow,
        [FromForm] string? ctaText,
        [FromForm] string? ctaUrl,
        [FromForm] int displayOrder,
        [FromForm] bool isActive,
        IFormFile? file,
        CancellationToken ct)
    {
        try
        {
            var existing = await _service.GetByIdAsync(id, ct);

            string? newUrl = existing.ImageUrl;
            if (file is not null && file.Length > 0)
            {
                newUrl = await _files.SaveAsync(file, Subfolder, ct);
                if (!string.IsNullOrEmpty(existing.ImageUrl))
                    await _files.DeleteAsync(existing.ImageUrl, ct);
            }

            await _service.UpdateAsync(id, new UpdateHeroBannerDto
            {
                Title = title?.Trim() ?? string.Empty,
                Subtitle = subtitle?.Trim(),
                Eyebrow = eyebrow?.Trim(),
                ImageUrl = newUrl,
                CtaText = ctaText?.Trim(),
                CtaUrl = ctaUrl?.Trim(),
                DisplayOrder = displayOrder,
                IsActive = isActive
            }, ct);
            TempData["Success"] = "Banner yeniləndi.";
        }
        catch (NotFoundException nex) { TempData["Error"] = nex.Message; }
        catch (ValidationException vex) { TempData["Error"] = string.Join(" • ", vex.Errors.SelectMany(kv => kv.Value)); }
        catch (BusinessException bex) { TempData["Error"] = bex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id, CancellationToken ct)
    {
        try { await _service.ToggleActiveAsync(id, ct); TempData["Success"] = "Banner statusu dəyişdi."; }
        catch (NotFoundException nex) { TempData["Error"] = nex.Message; }
        catch (BusinessException bex) { TempData["Error"] = bex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var existing = await _service.GetByIdAsync(id, ct);
            await _service.DeleteAsync(id, ct);
            if (!string.IsNullOrEmpty(existing.ImageUrl))
                await _files.DeleteAsync(existing.ImageUrl, ct);
            TempData["Success"] = "Banner silindi.";
        }
        catch (NotFoundException nex) { TempData["Error"] = nex.Message; }
        catch (BusinessException bex) { TempData["Error"] = bex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
