using Stylebazar.BLL.Constants;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Rules;

public class ReviewBusinessRules
{
    private readonly IReviewRepository _reviews;
    private readonly IProductRepository _products;

    public ReviewBusinessRules(IReviewRepository reviews, IProductRepository products)
    {
        _reviews = reviews;
        _products = products;
    }

    public async Task<Review> ReviewMustExistAsync(int id, CancellationToken ct = default)
    {
        Review? review = await _reviews.GetAsync(
            r => r.Id == id,
            enableTracking: false,
            cancellationToken: ct);

        if (review is null)
            throw new NotFoundException(Messages.Review.NotFound);

        return review;
    }

    public async Task ProductMustExistAsync(int productId, CancellationToken ct = default)
    {
        bool exists = await _products.AnyAsync(
            p => p.Id == productId,
            enableTracking: false,
            cancellationToken: ct);

        if (!exists)
            throw new NotFoundException(nameof(Product), productId);
    }

    public async Task UserHasNotReviewedYetAsync(string userId, int productId, CancellationToken ct = default)
    {
        bool already = await _reviews.AnyAsync(
            r => r.UserId == userId && r.ProductId == productId,
            enableTracking: false,
            cancellationToken: ct);

        if (already)
            throw new BusinessException(Messages.Review.AlreadyReviewed);
    }

    public void MustBeOwner(Review review, string? currentUserId, bool isAdmin)
    {
        if (isAdmin) return;

        if (string.IsNullOrEmpty(currentUserId))
            throw new UnauthorizedException(Messages.Review.NotAuthenticated);

        if (review.UserId != currentUserId)
            throw new BusinessException(Messages.Review.NotOwner);
    }
}
