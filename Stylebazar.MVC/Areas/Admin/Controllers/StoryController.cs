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
        int ok = 0;
        var failures = new List<string>();
        foreach (int id in ids)
        {
            try { await _stories.DeleteAsync(id, ct); ok++; }
            catch (NotFoundException ex) { failures.Add(ex.Message); }
            catch (BusinessException ex) { failures.Add(ex.Message); }
            catch (Exception ex) { failures.Add("Sistem xətası: " + ex.GetType().Name); }
        }
        if (failures.Count == 0)
            TempData["Success"] = $"{ok} hekayə silindi.";
        else
        {
            var grouped = failures.GroupBy(r => r).Select(g => $"{g.Count()}× {g.Key}");
            TempData["Error"] = $"{ok} silindi, {failures.Count} uğursuz: " + string.Join(" • ", grouped);
        }
        return RedirectToAction(nameof(Index));
    }
}
