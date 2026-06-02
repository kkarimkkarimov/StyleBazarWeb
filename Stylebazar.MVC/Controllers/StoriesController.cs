using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Stories;

namespace Stylebazar.MVC.Controllers;

// StoriesController — HTTP sorğularını qarşılayan MVC controller-i. İstəyi alır, biznes xidmətini (service) çağırır və nəticəni Razor view / JSON kimi qaytarır.
[Route("[controller]")]
public class StoriesController : Controller
{
    private readonly IStoryService _service;

    public StoriesController(IStoryService service)
    {
        _service = service;
    }

    /// <summary>
    /// Anonymous view tracking — called by the story viewer JS. Rate-limited
    /// (same policy as contact-click telemetry) so the view counter can't be
    /// inflated by hammering the endpoint.
    /// </summary>
    [HttpPost("View/{id:int}")]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> View(int id, CancellationToken ct)
    {
        try
        {
            await _service.RecordViewAsync(id, ct);
            return NoContent();
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
