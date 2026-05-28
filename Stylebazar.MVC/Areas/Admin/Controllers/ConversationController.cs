using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Chat;
using Stylebazar.MVC.Areas.Admin.ViewModels;

namespace Stylebazar.MVC.Areas.Admin.Controllers;

/// <summary>
/// Admin chat moderation. Read-only browse of every conversation in the
/// system; soft-delete individual messages from the audit view. Never sends
/// messages or impersonates users.
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ConversationController : Controller
{
    private readonly IChatService _chat;

    public ConversationController(IChatService chat) { _chat = chat; }

    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var result = await _chat.GetAllConversationsForAdminAsync(page, 25, ct);
        return View(new AdminConversationsPageViewModel { Conversations = result, Page = page });
    }

    public async Task<IActionResult> Detail(int id, int page = 1, CancellationToken ct = default)
    {
        // Re-fetch the full list to find the row metadata (peers, last-msg).
        // For a moderation view this is fine — page sizes are tiny.
        var all = await _chat.GetAllConversationsForAdminAsync(1, 1000, ct);
        var row = all.Items.FirstOrDefault(c => c.Id == id);
        if (row is null)
        {
            TempData["Error"] = "Söhbət tapılmadı.";
            return RedirectToAction(nameof(Index));
        }
        var messages = await _chat.GetMessagesForAdminAsync(id, page, 50, ct);
        return View(new AdminConversationDetailViewModel
        {
            Conversation = row,
            Messages = messages,
            Page = page
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessage(int id, int conversationId, CancellationToken ct)
    {
        try
        {
            await _chat.DeleteMessageForAdminAsync(id, ct);
            TempData["Success"] = "Mesaj gizlədildi.";
        }
        catch (NotFoundException ex) { TempData["Error"] = ex.Message; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id = conversationId });
    }
}
