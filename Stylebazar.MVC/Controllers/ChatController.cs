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

// ChatController — messenger UI-ni qidalandıran JSON REST API (V2).
// SignalR ChatHub eyni BLL-i paylaşır; bu endpoint-lər ilkin yükləmə, səhifələmə,
// fayl yükləmə və SignalR kəsiləndə fetch-fallback üçündür. Hər mutasiya SignalR
// ilə hər iki tərəfə YAYIMLANIR ki, ekranlar canlı yenilənsin.
// Hamısı auth-qapılı; yazma endpoint-ləri rate-limit-lidir (spam qoruması).
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

    // ---- Yayım köməkçiləri ----

    // Hadisəni həm göndərənin (digər tab-lar), həm qarşı tərəfin qrupuna ötürür.
    private async Task BroadcastToBothAsync(string? peerUserId, string eventName, object payload)
    {
        string me = _users.GetUserId(User) ?? string.Empty;
        if (!string.IsNullOrEmpty(me))
            await _hubContext.Clients.Group("user-" + me).SendAsync(eventName, payload);
        if (!string.IsNullOrEmpty(peerUserId) &&
            !string.Equals(peerUserId, me, StringComparison.OrdinalIgnoreCase))
        {
            await _hubContext.Clients.Group("user-" + peerUserId).SendAsync(eventName, payload);
        }
    }

    // ============== GƏLƏNLƏR QUTUSU ==============

    // Cari istifadəçinin söhbətləri (filtr + axtarış + səhifələmə).
    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations(
        [FromQuery] string? filter = null, [FromQuery] string? search = null,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var result = await _chat.GetMyConversationsAsync(filter, search, page, pageSize, ct);
        return Ok(result);
    }

    // İdempotent "bu istifadəçi ilə söhbət aç".
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

    // Mesaj tarixçəsi (ən yenisi öndə, səhifələnmiş).
    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<IActionResult> Messages(int conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30, CancellationToken ct = default)
    {
        try
        {
            var result = await _chat.GetMessagesAsync(conversationId, page, pageSize, ct);
            return Ok(result);
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException) { return Forbid(); }
    }

    // Sağ panel — söhbət detalı (peer + mağaza + paylaşılan media + şəxsi parametrlər).
    [HttpGet("conversations/{conversationId:int}/detail")]
    public async Task<IActionResult> Detail(int conversationId, CancellationToken ct)
    {
        try { return Ok(await _chat.GetConversationDetailAsync(conversationId, ct)); }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException) { return Forbid(); }
    }

    // Söhbət daxilində mətn axtarışı.
    [HttpGet("conversations/{conversationId:int}/search")]
    public async Task<IActionResult> SearchMessages(int conversationId, [FromQuery] string q, CancellationToken ct)
    {
        try { return Ok(await _chat.SearchMessagesAsync(conversationId, q, 20, ct)); }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException) { return Forbid(); }
    }

    // ============== GÖNDƏRMƏ ==============

    // Mətn / hekayə-cavabı / məhsul-paylaşımı göndər.
    [HttpPost("send")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> Send([FromBody] SendMessageDto dto, CancellationToken ct)
    {
        try
        {
            var msg = await _chat.SendMessageAsync(dto, ct);
            await BroadcastToBothAsync(dto.RecipientUserId, "MessageReceived", msg);
            return Ok(msg);
        }
        catch (ValidationException vex) { return UnprocessableEntity(new { errors = vex.Errors }); }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // Şəkil yüklə + Image mesajı göndər (bir gediş-gəliş).
    [HttpPost("send-image")]
    [EnableRateLimiting("write-default")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> SendImage(
        [FromForm] string recipientUserId,
        [FromForm] string? body,
        [FromForm] int? replyToMessageId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Şəkil seçilməyib." });
        if (file.Length > 10L * 1024 * 1024)
            return BadRequest(new { error = "Şəkil maksimum 10 MB ola bilər." });
        string mime = (file.ContentType ?? string.Empty).ToLowerInvariant();
        if (!mime.StartsWith("image/"))
            return BadRequest(new { error = "Yalnız şəkil göndərmək olar." });

        return await SaveAndSendAsync(file, new SendMessageDto
        {
            RecipientUserId = recipientUserId,
            MessageType = DAL.Entities.Enums.MessageType.Image,
            Body = body,
            ReplyToMessageId = replyToMessageId,
            AttachmentName = Path.GetFileName(file.FileName),
            AttachmentSize = file.Length
        }, ct);
    }

    // Səsli mesaj yüklə + Voice mesajı göndər.
    [HttpPost("send-voice")]
    [EnableRateLimiting("write-default")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> SendVoice(
        [FromForm] string recipientUserId,
        [FromForm] int durationSeconds,
        [FromForm] int? replyToMessageId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Səs yazısı yoxdur." });
        if (file.Length > 5L * 1024 * 1024)
            return BadRequest(new { error = "Səsli mesaj maksimum 5 MB ola bilər." });
        string mime = (file.ContentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
        if (!mime.StartsWith("audio/") && mime != "video/webm")
            return BadRequest(new { error = "Yanlış audio formatı." });

        return await SaveAndSendAsync(file, new SendMessageDto
        {
            RecipientUserId = recipientUserId,
            MessageType = DAL.Entities.Enums.MessageType.Voice,
            DurationSeconds = Math.Clamp(durationSeconds, 1, 300),
            ReplyToMessageId = replyToMessageId,
            AttachmentSize = file.Length
        }, ct);
    }

    // Fayl (PDF) yüklə + File mesajı göndər.
    [HttpPost("send-file")]
    [EnableRateLimiting("write-default")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> SendFile(
        [FromForm] string recipientUserId,
        [FromForm] string? body,
        [FromForm] int? replyToMessageId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Fayl seçilməyib." });
        if (file.Length > 10L * 1024 * 1024)
            return BadRequest(new { error = "Fayl maksimum 10 MB ola bilər." });
        string mime = (file.ContentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
        if (mime != "application/pdf")
            return BadRequest(new { error = "Hələlik yalnız PDF sənəd göndərmək olar." });

        return await SaveAndSendAsync(file, new SendMessageDto
        {
            RecipientUserId = recipientUserId,
            MessageType = DAL.Entities.Enums.MessageType.File,
            Body = body,
            ReplyToMessageId = replyToMessageId,
            AttachmentName = Path.GetFileName(file.FileName),
            AttachmentSize = file.Length
        }, ct);
    }

    // Ortaq: faylı saxla → DTO-ya URL yaz → göndər → yayımla.
    private async Task<IActionResult> SaveAndSendAsync(IFormFile file, SendMessageDto dto, CancellationToken ct)
    {
        try
        {
            dto.AttachmentUrl = await _files.SaveAsync(file, AttachmentSubfolder, ct);
            var msg = await _chat.SendMessageAsync(dto, ct);
            await BroadcastToBothAsync(dto.RecipientUserId, "MessageReceived", msg);
            return Ok(msg);
        }
        catch (ValidationException vex) { return UnprocessableEntity(new { errors = vex.Errors }); }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ============== REDAKTƏ / SİLMƏ / REAKSİYA ==============

    public record EditRequest(string Body);

    // Öz mətn mesajını redaktə et.
    [HttpPost("messages/{messageId:int}/edit")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> Edit(int messageId, [FromBody] EditRequest req, CancellationToken ct)
    {
        try
        {
            var msg = await _chat.EditMessageAsync(messageId, req.Body, ct);
            // Hər iki tərəfə bildir — bubble yerindəcə yenilənsin.
            await BroadcastToBothAsync(await PeerOfConversationAsync(msg.ConversationId, ct), "MessageEdited",
                new { conversationId = msg.ConversationId, messageId = msg.Id, body = msg.Body, editedAt = msg.EditedAt });
            return Ok(msg);
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // Öz mesajını sil (hamı üçün).
    [HttpPost("messages/{messageId:int}/delete")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> Delete(int messageId, CancellationToken ct)
    {
        try
        {
            (int convId, string peerId) = await _chat.DeleteMessageAsync(messageId, ct);
            await BroadcastToBothAsync(peerId, "MessageDeleted", new { conversationId = convId, messageId });
            return NoContent();
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    public record ReactRequest(string Emoji);

    // Reaksiya qoy/dəyiş/sil.
    [HttpPost("messages/{messageId:int}/react")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> React(int messageId, [FromBody] ReactRequest req, CancellationToken ct)
    {
        try
        {
            (var reactions, int convId, string peerId) = await _chat.ReactAsync(messageId, req.Emoji, ct);
            await BroadcastToBothAsync(peerId, "ReactionChanged",
                new { conversationId = convId, messageId, reactions });
            return Ok(new { reactions });
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ============== OXUNDU / SAYĞAC ==============

    // Söhbəti oxundu işarələ (qarşı tərəfə "Görüldü" siqnalı gedir).
    [HttpPost("conversations/{conversationId:int}/read")]
    public async Task<IActionResult> MarkRead(int conversationId, CancellationToken ct)
    {
        string? peerId = await _chat.MarkConversationReadAsync(conversationId, ct);
        string me = _users.GetUserId(User) ?? string.Empty;
        if (!string.IsNullOrEmpty(peerId) && !string.IsNullOrEmpty(me))
        {
            await _hubContext.Clients.Group("user-" + peerId)
                .SendAsync("MessagesRead", new { conversationId, readerUserId = me });
        }
        return NoContent();
    }

    // Header rozetkası üçün oxunmamış cəm.
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        int n = await _chat.GetMyUnreadTotalAsync(ct);
        return Ok(new { count = n });
    }

    // ============== SÖHBƏT PARAMETRLƏRİ ==============

    [HttpPost("conversations/{conversationId:int}/pin")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> TogglePin(int conversationId, CancellationToken ct)
        => await ToggleAsync(() => _chat.TogglePinAsync(conversationId, ct));

    [HttpPost("conversations/{conversationId:int}/mute")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> ToggleMute(int conversationId, CancellationToken ct)
        => await ToggleAsync(() => _chat.ToggleMuteAsync(conversationId, ct));

    [HttpPost("conversations/{conversationId:int}/fav")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> ToggleFav(int conversationId, CancellationToken ct)
        => await ToggleAsync(() => _chat.ToggleFavoriteAsync(conversationId, ct));

    // Ortaq toggle sarğısı — xətaları JSON-a çevirir.
    private async Task<IActionResult> ToggleAsync(Func<Task<bool>> action)
    {
        try { return Ok(new { value = await action() }); }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    public record NoteRequest(string? Note);
    public record TagRequest(string? Tag);

    [HttpPost("conversations/{conversationId:int}/note")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> SetNote(int conversationId, [FromBody] NoteRequest req, CancellationToken ct)
    {
        try { await _chat.SetNoteAsync(conversationId, req.Note, ct); return NoContent(); }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("conversations/{conversationId:int}/tag")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> SetTag(int conversationId, [FromBody] TagRequest req, CancellationToken ct)
    {
        try { await _chat.SetTagAsync(conversationId, req.Tag, ct); return NoContent(); }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ============== BLOK / ŞİKAYƏT ==============

    [HttpPost("block/{peerUserId}")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> ToggleBlock(string peerUserId, CancellationToken ct)
    {
        try { return Ok(new { blocked = await _chat.ToggleBlockAsync(peerUserId, ct) }); }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    public record ReportRequest(string PeerUserId, int? ConversationId, string Reason);

    [HttpPost("report")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> Report([FromBody] ReportRequest req, CancellationToken ct)
    {
        try
        {
            await _chat.ReportUserAsync(req.PeerUserId, req.ConversationId, req.Reason, ct);
            return NoContent();
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ============== SÜRƏTLİ CAVABLAR ==============

    [HttpGet("quick-replies")]
    public async Task<IActionResult> QuickReplies(CancellationToken ct)
        => Ok(await _chat.GetMyQuickRepliesAsync(ct));

    public record QuickReplyRequest(string Title, string Body);

    [HttpPost("quick-replies")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> AddQuickReply([FromBody] QuickReplyRequest req, CancellationToken ct)
    {
        try { return Ok(await _chat.AddQuickReplyAsync(req.Title, req.Body, ct)); }
        catch (BusinessException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("quick-replies/{id:int}")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> DeleteQuickReply(int id, CancellationToken ct)
    {
        await _chat.DeleteQuickReplyAsync(id, ct);
        return NoContent();
    }

    // Composer məhsul kartı üçün minimal məhsul məlumatı.
    [HttpGet("product-preview/{productId:int}")]
    public async Task<IActionResult> ProductPreview(int productId, CancellationToken ct)
    {
        var p = await _chat.GetProductPreviewAsync(productId, ct);
        return p is null ? NotFound(new { error = "Məhsul tapılmadı." }) : Ok(p);
    }

    // ============== KÖMƏKÇİ ==============

    // Redaktə yayımı üçün qarşı tərəfin id-sini detail sorğusu ilə tapır.
    private async Task<string?> PeerOfConversationAsync(int conversationId, CancellationToken ct)
    {
        try
        {
            var detail = await _chat.GetConversationDetailAsync(conversationId, ct);
            return detail.PeerUserId;
        }
        catch { return null; }
    }
}
