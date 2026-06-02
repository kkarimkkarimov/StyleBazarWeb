using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Reviews;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

// ReviewController (Admin paneli) — yalnız adminə açıq əməliyyatlar üçün controller. İstəyi alır, biznes xidmətini çağırır, idarəetmə nəticəsini/səhifəsini qaytarır.
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewController : Controller
{
    private readonly IReviewService _reviews;

    public ReviewController(IReviewService reviews) { _reviews = reviews; }

    public async Task<IActionResult> Index(int? maxRating, string? search, CancellationToken ct)
    {
        var rows = await _reviews.GetAllForAdminAsync(maxRating, search, take: 100, ct);
        return View(new AdminReviewsPageViewModel
        {
            Reviews = rows.Select(r => new ReviewAdminRow
            {
                Review = r.Review,
                ProductId = r.ProductId,
                ProductName = r.ProductName
            }).ToList(),
            RatingFilter = maxRating,
            Search = search
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try { await _reviews.DeleteAsync(id, ct); TempData["Success"] = "Rəy silindi."; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelete(int[] ids, CancellationToken ct)
    {
        if (ids is null || ids.Length == 0) { TempData["Error"] = "Heç bir rəy seçilməyib."; return RedirectToAction(nameof(Index)); }
        int ok = 0;
        var failures = new List<string>();
        foreach (int id in ids)
        {
            try { await _reviews.DeleteAsync(id, ct); ok++; }
            catch (NotFoundException ex) { failures.Add(ex.Message); }
            catch (BusinessException ex) { failures.Add(ex.Message); }
            catch (Exception ex) { failures.Add("Sistem xətası: " + ex.GetType().Name); }
        }
        if (failures.Count == 0)
            TempData["Success"] = $"{ok} rəy silindi.";
        else
        {
            var grouped = failures.GroupBy(r => r).Select(g => $"{g.Count()}× {g.Key}");
            TempData["Error"] = $"{ok} silindi, {failures.Count} uğursuz: " + string.Join(" • ", grouped);
        }
        return RedirectToAction(nameof(Index));
    }
}
