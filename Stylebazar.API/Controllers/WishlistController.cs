using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Wishlist;
using Stylebazar.BLL.Services.Wishlist;

namespace Stylebazar.API.Controllers;

// WishlistController — REST API controller. HTTP sorğusunu qəbul edir, biznes xidmətini çağırır və JSON cavab qaytarır.
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Buyer,Seller,Admin")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _service;

    public WishlistController(IWishlistService service)
    {
        _service = service;
    }

    /// <summary>The current user's wishlist.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WishlistItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<WishlistItemDto>>> GetMine(CancellationToken ct)
    {
        List<WishlistItemDto> items = await _service.GetMineAsync(ct);
        return Ok(items);
    }

    /// <summary>Add a product to the current user's wishlist.</summary>
    [HttpPost("{productId:int}")]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<WishlistItemDto>> Add(int productId, CancellationToken ct)
    {
        WishlistItemDto item = await _service.AddAsync(productId, ct);
        return StatusCode(StatusCodes.Status201Created, item);
    }

    /// <summary>Remove a product from the current user's wishlist.</summary>
    [HttpDelete("{productId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int productId, CancellationToken ct)
    {
        await _service.RemoveAsync(productId, ct);
        return NoContent();
    }
}
