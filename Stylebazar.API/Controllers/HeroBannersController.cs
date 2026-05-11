using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.HeroBanners;
using Stylebazar.BLL.Services.HeroBanners;

namespace Stylebazar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HeroBannersController : ControllerBase
{
    private readonly IHeroBannerService _service;

    public HeroBannersController(IHeroBannerService service)
    {
        _service = service;
    }

    /// <summary>Active banners for the public homepage.</summary>
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<HeroBannerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<HeroBannerDto>>> GetActive(CancellationToken ct)
    {
        List<HeroBannerDto> list = await _service.GetActiveAsync(ct);
        return Ok(list);
    }

    /// <summary>All banners (admin).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<HeroBannerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<HeroBannerDto>>> GetAll(CancellationToken ct)
    {
        List<HeroBannerDto> list = await _service.GetAllAsync(ct);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(HeroBannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HeroBannerDto>> GetById(int id, CancellationToken ct)
    {
        HeroBannerDto dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(HeroBannerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<HeroBannerDto>> Create([FromBody] CreateHeroBannerDto dto, CancellationToken ct)
    {
        HeroBannerDto created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(HeroBannerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HeroBannerDto>> Update(int id, [FromBody] UpdateHeroBannerDto dto, CancellationToken ct)
    {
        HeroBannerDto updated = await _service.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
