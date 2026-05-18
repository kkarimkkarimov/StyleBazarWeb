using Core.Persistence.Paging;
using Microsoft.AspNetCore.Http;
using Stylebazar.BLL.DTOs.Reviews;

namespace Stylebazar.BLL.Services.Reviews;

public interface IReviewService
{
    Task<Paginate<ReviewDto>> GetByProductAsync(int productId, ReviewListQueryDto query, CancellationToken ct = default);
    Task<ReviewDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ReviewDto> CreateAsync(int productId, CreateReviewDto dto, CancellationToken ct = default);
    Task<ReviewDto> UpdateAsync(int id, UpdateReviewDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    Task<ReviewImageDto> AddImageAsync(int reviewId, IFormFile file, CancellationToken ct = default);
    Task DeleteImageAsync(int reviewId, int imageId, CancellationToken ct = default);

    /// <summary>
    /// Returns the current user's review for the given product (or null when the
    /// user is anonymous or has not reviewed it yet). Used by the product
    /// detail page to render the "Sənin rəyin" card with edit/delete actions
    /// and to hide the "Rəy yaz" CTA when a review already exists.
    /// </summary>
    Task<ReviewDto?> GetMineForProductAsync(int productId, CancellationToken ct = default);

    /// <summary>
    /// Admin-only: returns recent reviews across every product with product
    /// name attached. Filters by max rating + comment search. Hard-capped.
    /// </summary>
    Task<List<AdminReviewRowDto>> GetAllForAdminAsync(int? maxRating = null, string? search = null, int take = 100, CancellationToken ct = default);
}

public class AdminReviewRowDto
{
    public ReviewDto Review { get; set; } = new();
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
}
