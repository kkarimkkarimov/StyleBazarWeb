using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Cart;
using Stylebazar.BLL.Services.Cart;

namespace Stylebazar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Buyer,Seller,Admin")]
public class CartController : ControllerBase
{
    private readonly ICartService _service;

    public CartController(ICartService service)
    {
        _service = service;
    }

    /// <summary>Get the current user's cart (auto-created on first access).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> GetMine(CancellationToken ct)
    {
        CartDto cart = await _service.GetMineAsync(ct);
        return Ok(cart);
    }

    /// <summary>Add an item to the cart (merges into existing line for same product+size).</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CartDto>> Add([FromBody] AddToCartDto dto, CancellationToken ct)
    {
        CartDto cart = await _service.AddAsync(dto, ct);
        return Ok(cart);
    }

    /// <summary>Replace the quantity on an existing line.</summary>
    [HttpPatch("items/{itemId:int}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CartDto>> Update(int itemId, [FromBody] UpdateCartItemDto dto, CancellationToken ct)
    {
        CartDto cart = await _service.UpdateQuantityAsync(itemId, dto, ct);
        return Ok(cart);
    }

    /// <summary>Remove a line from the cart.</summary>
    [HttpDelete("items/{itemId:int}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CartDto>> Remove(int itemId, CancellationToken ct)
    {
        CartDto cart = await _service.RemoveAsync(itemId, ct);
        return Ok(cart);
    }

    /// <summary>Empty the entire cart.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await _service.ClearAsync(ct);
        return NoContent();
    }

    /// <summary>Lightweight count for the header badge.</summary>
    [HttpGet("count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyCount(CancellationToken ct)
    {
        int count = await _service.GetMyCountAsync(ct);
        return Ok(new { count });
    }
}
