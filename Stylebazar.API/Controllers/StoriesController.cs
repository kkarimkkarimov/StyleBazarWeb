using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Stories;
using Stylebazar.BLL.Services.Stories;

namespace Stylebazar.API.Controllers;

[ApiController]
[Produces("application/json")]
public class StoriesController : ControllerBase
{
    private readonly IStoryService _service;

    public StoriesController(IStoryService service)
    {
        _service = service;
    }

    /// <summary>Active stories grouped by store, for the home rail.</summary>
    [HttpGet("api/stories/active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<StoreStoriesDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StoreStoriesDto>>> GetActiveFeed(CancellationToken ct)
    {
        List<StoreStoriesDto> feed = await _service.GetActiveFeedAsync(ct);
        return Ok(feed);
    }

    /// <summary>All stories for a specific store (active by default).</summary>
    [HttpGet("api/stores/{storeId:int}/stories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<StoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StoryDto>>> GetByStore(int storeId, [FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        List<StoryDto> stories = await _service.GetByStoreAsync(storeId, activeOnly, ct);
        return Ok(stories);
    }

    /// <summary>Publish a story for a store. Owner of that store or Admin.</summary>
    [HttpPost("api/stores/{storeId:int}/stories")]
    [Authorize(Roles = "Seller,Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(40 * 1024 * 1024)]  // 30MB video + headroom for form fields
    [ProducesResponseType(typeof(StoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StoryDto>> Create(
        int storeId,
        IFormFile file,
        [FromForm] CreateStoryDto dto,
        CancellationToken ct)
    {
        StoryDto created = await _service.CreateAsync(storeId, dto, file, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Record a view (anonymous, idempotent).</summary>
    [HttpPost("api/stories/{id:int}/view")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoryDto>> RecordView(int id, CancellationToken ct)
    {
        StoryDto dto = await _service.RecordViewAsync(id, ct);
        return Ok(dto);
    }

    /// <summary>Delete (permanent + disk cleanup). Owner or Admin.</summary>
    [HttpDelete("api/stories/{id:int}")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
