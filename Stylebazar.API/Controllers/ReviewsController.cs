using Core.Persistence.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Reviews;
using Stylebazar.BLL.Services.Reviews;

namespace Stylebazar.API.Controllers;

// ReviewsController — REST API controller. HTTP sorğusunu qəbul edir, biznes xidmətini çağırır və JSON cavab qaytarır.
[ApiController]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _service;

    public ReviewsController(IReviewService service)
    {
        _service = service;
    }

    /// <summary>List reviews for a product (paginated, optional rating filter, sort).</summary>
    [HttpGet("api/products/{productId:int}/reviews")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Paginate<ReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Paginate<ReviewDto>>> GetByProduct(
        int productId,
        [FromQuery] ReviewListQueryDto query,
        CancellationToken ct)
    {
        Paginate<ReviewDto> result = await _service.GetByProductAsync(productId, query, ct);
        return Ok(result);
    }

    /// <summary>Create a review for a product. Buyer (or any authenticated user).</summary>
    [HttpPost("api/products/{productId:int}/reviews")]
    [Authorize(Roles = "Buyer,Seller,Admin")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReviewDto>> Create(
        int productId,
        [FromBody] CreateReviewDto dto,
        CancellationToken ct)
    {
        ReviewDto review = await _service.CreateAsync(productId, dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = review.Id }, review);
    }

    [HttpGet("api/reviews/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> GetById(int id, CancellationToken ct)
    {
        ReviewDto dto = await _service.GetByIdAsync(id, ct);
        return Ok(dto);
    }

    /// <summary>Update own review (or any review if Admin).</summary>
    [HttpPut("api/reviews/{id:int}")]
    [Authorize(Roles = "Buyer,Seller,Admin")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReviewDto>> Update(int id, [FromBody] UpdateReviewDto dto, CancellationToken ct)
    {
        ReviewDto updated = await _service.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    /// <summary>Soft-delete own review (or any review if Admin).</summary>
    [HttpDelete("api/reviews/{id:int}")]
    [Authorize(Roles = "Buyer,Seller,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("api/reviews/{id:int}/images")]
    [Authorize(Roles = "Buyer,Seller,Admin")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [ProducesResponseType(typeof(ReviewImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReviewImageDto>> AddImage(int id, IFormFile file, CancellationToken ct)
    {
        ReviewImageDto img = await _service.AddImageAsync(id, file, ct);
        return StatusCode(StatusCodes.Status201Created, img);
    }

    [HttpDelete("api/reviews/{id:int}/images/{imageId:int}")]
    [Authorize(Roles = "Buyer,Seller,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken ct)
    {
        await _service.DeleteImageAsync(id, imageId, ct);
        return NoContent();
    }
}
