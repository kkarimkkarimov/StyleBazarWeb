using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Stylebazar.BLL.DTOs.Chat;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Chat;
using Stylebazar.BLL.Services.FileStorage;
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

    public ChatController(IChatService chat, IFileStorageService files)
    {
        _chat = chat;
        _files = files;
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
        await _chat.MarkConversationReadAsync(conversationId, ct);
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
