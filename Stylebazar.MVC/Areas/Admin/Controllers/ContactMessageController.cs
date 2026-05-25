using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Contact;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Contact;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ContactMessageController : Controller
{
    private readonly IContactMessageService _service;

    public ContactMessageController(IContactMessageService service) { _service = service; }

    public async Task<IActionResult> Index(ContactStatus? status, int page = 1, CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(page, 25, status, ct);
        return View(new AdminContactMessagesPageViewModel
        {
            Messages = result, StatusFilter = status, Page = page
        });
    }

    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        try
        {
            var msg = await _service.GetByIdAsync(id, ct);
            return View(msg);
        }
        catch (NotFoundException)
        {
            TempData["Error"] = "Mesaj tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, ContactStatus status, string? adminNote, string? returnTo, CancellationToken ct)
    {
        try
        {
            await _service.UpdateStatusAsync(id, new UpdateContactMessageStatusDto
            {
                Status = status, AdminNote = adminNote
            }, ct);
            TempData["Success"] = "Status yeniləndi.";
        }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return returnTo == "detail"
            ? RedirectToAction(nameof(Detail), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try { await _service.DeleteAsync(id, ct); TempData["Success"] = "Mesaj silindi."; }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAction(int[] ids, string action, CancellationToken ct)
    {
        if (ids is null || ids.Length == 0) { TempData["Error"] = "Heç bir mesaj seçilməyib."; return RedirectToAction(nameof(Index)); }
        int ok = 0;
        var failures = new List<string>();
        foreach (int id in ids)
        {
            try
            {
                if (action == "read")
                    await _service.UpdateStatusAsync(id, new UpdateContactMessageStatusDto { Status = ContactStatus.InProgress }, ct);
                else if (action == "close")
                    await _service.UpdateStatusAsync(id, new UpdateContactMessageStatusDto { Status = ContactStatus.Resolved }, ct);
                else if (action == "delete")
                    await _service.DeleteAsync(id, ct);
                ok++;
            }
            catch (NotFoundException ex) { failures.Add(ex.Message); }
            catch (BusinessException ex) { failures.Add(ex.Message); }
            catch (Exception ex) { failures.Add("Sistem xətası: " + ex.GetType().Name); }
        }
        if (failures.Count == 0)
            TempData["Success"] = $"{ok} mesaj icra olundu.";
        else
        {
            var grouped = failures.GroupBy(r => r).Select(g => $"{g.Count()}× {g.Key}");
            TempData["Error"] = $"{ok} icra olundu, {failures.Count} uğursuz: " + string.Join(" • ", grouped);
        }
        return RedirectToAction(nameof(Index));
    }
}
