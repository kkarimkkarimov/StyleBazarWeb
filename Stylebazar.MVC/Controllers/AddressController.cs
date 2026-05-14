using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Addresses;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Addresses;
using Stylebazar.MVC.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// MVC face of the address book. /Account/Addresses → full page, all
/// mutations are JSON AJAX endpoints so the page can update in place
/// without a full reload.
/// </summary>
[Authorize]
[Route("Account/[action]")]
public class AddressController : Controller
{
    private readonly IAddressService _addresses;

    public AddressController(IAddressService addresses)
    {
        _addresses = addresses;
    }

    [HttpGet]
    public async Task<IActionResult> Addresses(CancellationToken ct)
    {
        var items = await _addresses.GetMineAsync(ct);
        return View("Addresses", new AddressesIndexViewModel { Items = items });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAddress([FromForm] CreateAddressDto dto, CancellationToken ct)
    {
        try
        {
            AddressDto created = await _addresses.CreateAsync(dto, ct);
            return Json(new { ok = true, address = created });
        }
        catch (ValidationException ex) { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAddress([FromForm] int id, [FromForm] UpdateAddressDto dto, CancellationToken ct)
    {
        try
        {
            AddressDto updated = await _addresses.UpdateAsync(id, dto, ct);
            return Json(new { ok = true, address = updated });
        }
        catch (ValidationException ex) { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (NotFoundException ex)   { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress([FromForm] int id, CancellationToken ct)
    {
        try
        {
            await _addresses.DeleteAsync(id, ct);
            return Json(new { ok = true });
        }
        catch (NotFoundException ex) { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex) { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefaultAddress([FromForm] int id, CancellationToken ct)
    {
        try
        {
            await _addresses.SetDefaultAsync(id, ct);
            return Json(new { ok = true });
        }
        catch (NotFoundException ex) { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex) { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }
}
