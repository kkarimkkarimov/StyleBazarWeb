using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Stylebazar.BLL.DTOs.Chat;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Chat;
using Stylebazar.BLL.Services.FileStorage;
using Stylebazar.DAL.Entities;
using Stylebazar.MVC.Hubs;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// JSON REST endpoints powering the chat UI (floating widget + Messages page).
/// SignalR ChatHub (Phase 14.5) shares the same BLL — these endpoints exist
/// for the initial inbox load, pagination, image upload, and a fetch fallback
/// when the SignalR connection is down.
///
/// Everything here is auth-gated; rate-limited on the send endpoint to defuse
/// spam.
/// </summary>
[Authorize]
[ApiController]
[Route("api/chat")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private const string AttachmentSubfolder = "chat-attachments";

    private readonly IChatService _chat;
    private readonly IFileStorageService _files;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly UserManager<AppUser> _users;

    public ChatController(
        IChatService chat,
        IFileStorageService files,
        IHubContext<ChatHub> hubContext,
        UserManager<AppUser> users)
    {
        _chat = chat;
        _files = files;
        _hubContext = hubContext;
        _users = users;
    }

    /// <summary>
    /// Pushes the saved message to BOTH the sender's group (other tabs) AND
    /// the recipient's group via SignalR. Mirrors what ChatHub.SendMessage
    /// does — so a REST-sent message still arrives live on the peer's screen.
    /// </summary>
    private async Task BroadcastAsync(MessageDto saved, string? recipientUserId)
    {
        string me = _users.GetUserId(User) ?? string.Empty;
        if (!string.IsNullOrEmpty(me))
            await _hubContext.Clients.Group("user-" + me).SendAsync("MessageReceived", saved);
        if (!string.IsNullOrEmpty(recipientUserId) &&
            !string.Equals(recipientUserId, me, StringComparison.OrdinalIgnoreCase))
        {
            await _hubContext.Clients.Group("user-" + recipientUserId).SendAsync("MessageReceived", saved);
        }
    }

    /// <summary>Current user's inbox (paginated).</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await _chat.GetMyConversationsAsync(page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>Idempotent "open chat with this user".</summary>
    [HttpPost("conversations/with/{peerUserId}")]
    public async Task<IActionResult> OpenWith(string peerUserId, CancellationToken ct)
    {
        try
        {
            var conv = await _chat.GetOrCreateConversationAsync(peerUserId, ct);
            return Ok(conv);
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Message history for a conversation (newest-first, paginated).</summary>
    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> Messages(int conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
    {
        try
        {
            var result = await _chat.GetMessagesAsync(conversationId, page, pageSize, ct);
            return Ok(result);
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException) { return Forbid(); }  // not a participant
    }

    /// <summary>Send a message (text / image / story-reply / product-share).</summary>
    [HttpPost("send")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> Send([FromBody] SendMessageDto dto, CancellationToken ct)
    {
        try
        {
            var msg = await _chat.SendMessageAsync(dto, ct);
            // CRITICAL: REST-sent message must also fan out via SignalR so the
            // recipient's connected widget receives the live MessageReceived
            // event. Without this, REST send saves to DB but the peer only
            // sees it on next page-refresh / unread-count poll.
            await BroadcastAsync(msg, dto.RecipientUserId);
            return Ok(msg);
        }
        catch (ValidationException vex)
        {
            return UnprocessableEntity(new { errors = vex.Errors });
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>
    /// Upload an image attachment, then send a Message of type=Image referring
    /// to the saved URL. One round-trip vs. separate upload + send.
    /// </summary>
    [HttpPost("send-image")]
    [EnableRateLimiting("write-default")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> SendImage(
        [FromForm] string recipientUserId,
        [FromForm] string? body,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Şəkil seçilməyib." });
        // Same 10MB cap as story image — we don't need video here.
        if (file.Length > 10L * 1024 * 1024)
            return BadRequest(new { error = "Şəkil maksimum 10 MB ola bilər." });
        string ct2 = (file.ContentType ?? string.Empty).ToLowerInvariant();
        if (!ct2.StartsWith("image/"))
            return BadRequest(new { error = "Yalnız şəkil göndərmək olar." });

        string url = await _files.SaveAsync(file, AttachmentSubfolder, ct);

        try
        {
            var msg = await _chat.SendMessageAsync(new SendMessageDto
            {
                RecipientUserId = recipientUserId,
                MessageType = DAL.Entities.Enums.MessageType.Image,
                Body = body,
                AttachmentUrl = url
            }, ct);
            await BroadcastAsync(msg, recipientUserId);
            return Ok(msg);
        }
        catch (ValidationException vex) { return UnprocessableEntity(new { errors = vex.Errors }); }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Mark all messages in a conversation as read.</summary>
    [HttpPost("conversations/{conversationId:int}/read")]
    public async Task<IActionResult> MarkRead(int conversationId, CancellationToken ct)
    {
        string? peerId = await _chat.MarkConversationReadAsync(conversationId, ct);
        // Tell ONLY the peer (sender) their messages were seen — drives the
        // "Görüldü" indicator live without a refresh. Targeting the peer's group
        // avoids broadcasting read receipts to every connected client.
        string me = _users.GetUserId(User) ?? string.Empty;
        if (!string.IsNullOrEmpty(peerId) && !string.IsNullOrEmpty(me))
        {
            await _hubContext.Clients.Group("user-" + peerId).SendAsync("MessagesRead", new { conversationId, readerUserId = me });
        }
        return NoContent();
    }

    /// <summary>Unread total for the header badge.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        int n = await _chat.GetMyUnreadTotalAsync(ct);
        return Ok(new { count = n });
    }
}
