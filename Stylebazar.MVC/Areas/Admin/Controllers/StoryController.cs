using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Stories;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StoryController : Controller
{
    private readonly IStoryService _stories;

    public StoryController(IStoryService stories) { _stories = stories; }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var rows = await _stories.GetAllForAdminAsync(take: 200, ct);
        return View(new AdminStoriesPageViewModel
        {
            Stories = rows.Select(r => new StoryAdminRow
            {
                Story = r.Story, StoreName = r.StoreName, StoreSlug = r.StoreSlug
            }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try { await _stories.DeleteAsync(id, ct); TempData["Success"] = "Hekayə silindi."; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelete(int[] ids, CancellationToken ct)
    {
        if (ids is null || ids.Length == 0) { TempData["Error"] = "Heç bir hekayə seçilməyib."; return RedirectToAction(nameof(Index)); }
        int ok = 0, fail = 0;
        foreach (int id in ids)
        {
            try { await _stories.DeleteAsync(id, ct); ok++; }
            catch { fail++; }
        }
        TempData[fail == 0 ? "Success" : "Error"] = $"{ok} silindi, {fail} uğursuz.";
        return RedirectToAction(nameof(Index));
    }
}
