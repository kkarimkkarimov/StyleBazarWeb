using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Addresses;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Addresses;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// JSON-only endpoints for the per-user address book. Consumed by both
/// /Account/Addresses (full page) and the Seller Profile address card.
/// All actions require the user to be signed in — owner checks happen in the
/// service layer.
/// </summary>
[Authorize]
[Route("[controller]/[action]")]
public class AddressController : Controller
{
    private readonly IAddressService _addresses;

    public AddressController(IAddressService addresses)
    {
        _addresses = addresses;
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct)
    {
        var list = await _addresses.GetMineAsync(ct);
        return Json(new { ok = true, items = list });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] CreateAddressDto dto, CancellationToken ct)
    {
        try
        {
            var created = await _addresses.CreateAsync(dto, ct);
            return Json(new { ok = true, item = created });
        }
        catch (ValidationException ex)  { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (UnauthorizedException ex){ return Unauthorized(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)    { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] int id, [FromForm] UpdateAddressDto dto, CancellationToken ct)
    {
        try
        {
            var updated = await _addresses.UpdateAsync(id, dto, ct);
            return Json(new { ok = true, item = updated });
        }
        catch (NotFoundException ex)    { return NotFound(new { ok = false, error = ex.Message }); }
        catch (ValidationException ex)  { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (UnauthorizedException ex){ return Unauthorized(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)    { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] int id, CancellationToken ct)
    {
        try
        {
            await _addresses.DeleteAsync(id, ct);
            return Json(new { ok = true });
        }
        catch (NotFoundException ex)    { return NotFound(new { ok = false, error = ex.Message }); }
        catch (UnauthorizedException ex){ return Unauthorized(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)    { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault([FromForm] int id, CancellationToken ct)
    {
        try
        {
            var updated = await _addresses.SetDefaultAsync(id, ct);
            return Json(new { ok = true, item = updated });
        }
        catch (NotFoundException ex)    { return NotFound(new { ok = false, error = ex.Message }); }
        catch (UnauthorizedException ex){ return Unauthorized(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)    { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }
}
