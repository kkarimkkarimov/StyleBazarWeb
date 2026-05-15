using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.MVC.ViewModels;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// Public shop catalog: list of all active sellers + a single store profile.
/// No auth required for either action.
/// </summary>
public class StoreController : Controller
{
    private readonly IStoreService _stores;

    public StoreController(IStoreService stores)
    {
        _stores = stores;
    }

    /// <summary>/Store — the "Shops" tab. Grid of stores with live search.</summary>
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken ct = default)
    {
        var result = await _stores.GetListAsync(new StoreListQueryDto
        {
            Page = page,
            PageSize = 24,
            Search = search,
            IsActive = true
        }, ct);

        return View(new ShopsIndexViewModel
        {
            Stores = result,
            Search = search,
            Page = page
        });
    }

    /// <summary>/Store/Detail/{slug} — single store profile page. (Stub view in Phase 6.)</summary>
    [Route("Store/Detail/{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken ct)
    {
        try
        {
            var store = await _stores.GetBySlugAsync(slug, ct);
            return View(store);
        }
        catch (NotFoundException) { return NotFound(); }
    }
}
