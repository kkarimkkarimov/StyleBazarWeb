using Core.Persistence.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Newsletter;
using Stylebazar.BLL.Services.Newsletter;

namespace Stylebazar.API.Controllers;

// NewsletterController — REST API controller. HTTP sorğusunu qəbul edir, biznes xidmətini çağırır və JSON cavab qaytarır.
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NewsletterController : ControllerBase
{
    private readonly INewsletterService _service;

    public NewsletterController(INewsletterService service)
    {
        _service = service;
    }

    /// <summary>Subscribe an email. Anonymous; idempotent (reactivates if previously unsubscribed).</summary>
    [HttpPost("subscribe")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(NewsletterSubscriberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NewsletterSubscriberDto>> Subscribe([FromBody] SubscribeDto dto, CancellationToken ct)
    {
        NewsletterSubscriberDto result = await _service.SubscribeAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Unsubscribe via the unsubscribe-token sent in mail.</summary>
    [HttpGet("unsubscribe/{token}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unsubscribe(string token, CancellationToken ct)
    {
        await _service.UnsubscribeAsync(token, ct);
        return NoContent();
    }

    /// <summary>Admin: list subscribers.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Paginate<NewsletterSubscriberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Paginate<NewsletterSubscriberDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] bool? isActive = null,
        CancellationToken ct = default)
    {
        Paginate<NewsletterSubscriberDto> result = await _service.GetListAsync(page, pageSize, isActive, ct);
        return Ok(result);
    }
}
