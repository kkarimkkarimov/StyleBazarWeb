using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Reviews;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Reviews;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// AJAX endpoints used by the product-detail review form. All actions return
/// JSON; redirects/full pages are handled by ShopController.Detail.
/// </summary>
[Authorize]
public class ReviewController : Controller
{
    private readonly IReviewService _reviews;

    public ReviewController(IReviewService reviews)
    {
        _reviews = reviews;
    }

    /// <summary>
    /// POST /Review/Create — body is application/x-www-form-urlencoded
    /// (productId, rating, comment). Image uploads happen on a separate
    /// endpoint so the form works without JS too.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [FromForm] int productId,
        [FromForm] int rating,
        [FromForm] string? comment,
        CancellationToken ct)
    {
        try
        {
            var dto = new CreateReviewDto { Rating = rating, Comment = comment };
            var review = await _reviews.CreateAsync(productId, dto, ct);
            return Json(new { ok = true, review });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { ok = false, errors = ex.Errors });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { ok = false, error = ex.Message });
        }
        catch (BusinessException ex)
        {
            // e.g. "Bu məhsula artıq rəy yazmısınız."
            return UnprocessableEntity(new { ok = false, error = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        [FromForm] int id,
        [FromForm] int rating,
        [FromForm] string? comment,
        CancellationToken ct)
    {
        try
        {
            var dto = new UpdateReviewDto { Rating = rating, Comment = comment };
            var review = await _reviews.UpdateAsync(id, dto, ct);
            return Json(new { ok = true, review });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { ok = false, errors = ex.Errors });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { ok = false, error = ex.Message });
        }
        catch (BusinessException)
        {
            return Forbid();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([FromForm] int id, CancellationToken ct)
    {
        try
        {
            await _reviews.DeleteAsync(id, ct);
            return Json(new { ok = true });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { ok = false, error = ex.Message });
        }
        catch (BusinessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// POST /Review/UploadImage — multipart-form with reviewId + file. The
    /// review must already exist (created via /Review/Create) so we have an
    /// id to attach images to. Used after the form's successful submit.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
        [FromForm] int reviewId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { ok = false, error = "Fayl seçilməyib." });

        try
        {
            var image = await _reviews.AddImageAsync(reviewId, file, ct);
            return Json(new { ok = true, image });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { ok = false, errors = ex.Errors });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { ok = false, error = ex.Message });
        }
        catch (BusinessException)
        {
            return Forbid();
        }
    }
}
