using Core.Persistence.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Services.Stores;

namespace Stylebazar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _service;

    public StoresController(IStoreService service)
    {
        _service = service;
    }

    /// <summary>Public list of stores with filters and pagination. Anonymous callers see only active stores by default.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Paginate<StoreListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Paginate<StoreListDto>>> GetList(
        [FromQuery] StoreListQueryDto query,
        CancellationToken ct)
    {
        // Default public view: hide inactive stores unless explicitly requested.
        if (!User.IsInRole("Admin") && !query.IsActive.HasValue)
            query.IsActive = true;

        Paginate<StoreListDto> result = await _service.GetListAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Stores owned by the current user. Sellers only.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Seller")]
    [ProducesResponseType(typeof(List<StoreListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<StoreListDto>>> GetMine(CancellationToken ct)
    {
        List<StoreListDto> stores = await _service.GetMyStoresAsync(ct);
        return Ok(stores);
    }

    /// <summary>Get a store by id.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StoreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreDetailDto>> GetById(int id, CancellationToken ct)
    {
        StoreDetailDto dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    /// <summary>Get a store by URL slug.</summary>
    [HttpGet("by-slug/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StoreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreDetailDto>> GetBySlug(string slug, CancellationToken ct)
    {
        StoreDetailDto dto = await _service.GetBySlugAsync(slug, ct);
        return Ok(dto);
    }

    /// <summary>Create a new store. Sellers only. Owner is the current user.</summary>
    [HttpPost]
    [Authorize(Roles = "Seller")]
    [ProducesResponseType(typeof(StoreDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StoreDetailDto>> Create(
        [FromBody] CreateStoreDto dto,
        CancellationToken ct)
    {
        StoreDetailDto created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update a store. Owner or Admin.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(typeof(StoreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StoreDetailDto>> Update(
        int id,
        [FromBody] UpdateStoreDto dto,
        CancellationToken ct)
    {
        StoreDetailDto updated = await _service.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    /// <summary>Soft-delete a store. Owner or Admin. Fails if the store has products.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Seller,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    /// <summary>Toggle the IsActive flag (admin moderation).</summary>
    [HttpPatch("{id:int}/toggle-active")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(StoreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StoreDetailDto>> ToggleActive(int id, CancellationToken ct)
    {
        StoreDetailDto result = await _service.ToggleActiveAsync(id, ct);
        return Ok(result);
    }
}
