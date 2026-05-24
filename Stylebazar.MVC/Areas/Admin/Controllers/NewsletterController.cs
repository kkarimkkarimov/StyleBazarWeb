using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Services.Newsletter;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class NewsletterController : Controller
{
    private readonly INewsletterService _service;

    public NewsletterController(INewsletterService service) { _service = service; }

    public async Task<IActionResult> Index(bool? isActive, int page = 1, CancellationToken ct = default)
    {
        var result = await _service.GetListAsync(page, 50, isActive, ct);
        return View(new AdminNewsletterPageViewModel
        {
            Subscribers = result, ActiveFilter = isActive, Page = page
        });
    }

    public async Task<IActionResult> ExportCsv(bool? isActive, CancellationToken ct)
    {
        // Respect whatever filter the user has applied (or "all" if none); used
        // to default to active-only which was confusing because the page shows
        // unfiltered by default. Export reflects what the user is looking at.
        var result = await _service.GetListAsync(1, 10_000, isActive, ct);
        StringBuilder sb = new();
        // UTF-8 BOM so Excel renders Azerbaijani diacritics correctly.
        sb.AppendLine("Email,SubscribedAt,IsActive");
        foreach (var s in result.Items)
            sb.AppendLine($"{s.Email},{s.CreatedDate:O},{s.IsActive}");

        byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
        byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] full = new byte[bom.Length + body.Length];
        Buffer.BlockCopy(bom, 0, full, 0, bom.Length);
        Buffer.BlockCopy(body, 0, full, bom.Length, body.Length);

        string suffix = isActive switch { true => "-active", false => "-inactive", _ => "-all" };
        return File(full, "text/csv", $"stylebazar-newsletter{suffix}.csv");
    }
}
