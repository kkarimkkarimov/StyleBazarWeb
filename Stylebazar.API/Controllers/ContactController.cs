using Core.Persistence.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Contact;
using Stylebazar.BLL.Services.Contact;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ContactController : ControllerBase
{
    private readonly IContactMessageService _service;

    public ContactController(IContactMessageService service)
    {
        _service = service;
    }

    /// <summary>Submit a contact-form message. Anonymous.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ContactMessageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContactMessageDto>> Submit([FromBody] CreateContactMessageDto dto, CancellationToken ct)
    {
        string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        ContactMessageDto created = await _service.SubmitAsync(dto, ip, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    /// <summary>Admin: list submitted messages with optional status filter.</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(Paginate<ContactMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<Paginate<ContactMessageDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ContactStatus? status = null,
        CancellationToken ct = default)
    {
        Paginate<ContactMessageDto> result = await _service.GetListAsync(page, pageSize, status, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ContactMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactMessageDto>> GetById(int id, CancellationToken ct)
    {
        ContactMessageDto dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ContactMessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactMessageDto>> UpdateStatus(int id, [FromBody] UpdateContactMessageStatusDto dto, CancellationToken ct)
    {
        ContactMessageDto updated = await _service.UpdateStatusAsync(id, dto, ct);
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
