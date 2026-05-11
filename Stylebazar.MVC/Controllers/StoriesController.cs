using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Stories;

namespace Stylebazar.MVC.Controllers;

[Route("[controller]")]
public class StoriesController : Controller
{
    private readonly IStoryService _service;

    public StoriesController(IStoryService service)
    {
        _service = service;
    }

    /// <summary>Anonymous view tracking — called by the story viewer JS.</summary>
    [HttpPost("View/{id:int}")]
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
