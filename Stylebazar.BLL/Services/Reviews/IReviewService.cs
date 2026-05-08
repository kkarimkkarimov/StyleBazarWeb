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
}
