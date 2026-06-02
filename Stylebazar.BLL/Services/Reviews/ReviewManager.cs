using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Reviews;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Rules;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.BLL.Services.FileStorage;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Reviews;

// ReviewManager — xidmətin GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı). Yoxlama → repository/baza → nəticə axını burada idarə olunur.
public class ReviewManager : IReviewService
{
    private readonly IReviewRepository _reviews;
    private readonly IReviewImageRepository _images;
    private readonly IMapper _mapper;
    private readonly ReviewBusinessRules _rules;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<CreateReviewDto> _createValidator;
    private readonly IValidator<UpdateReviewDto> _updateValidator;

    private const string ImagesSubfolder = "reviews";

    public ReviewManager(
        IReviewRepository reviews,
        IReviewImageRepository images,
        IMapper mapper,
        ReviewBusinessRules rules,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        IValidator<CreateReviewDto> createValidator,
        IValidator<UpdateReviewDto> updateValidator)
    {
        _reviews = reviews;
        _images = images;
        _mapper = mapper;
        _rules = rules;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Paginate<ReviewDto>> GetByProductAsync(int productId, ReviewListQueryDto query, CancellationToken ct = default)
    {
        await _rules.ProductMustExistAsync(productId, ct);

        Paginate<Review> page = await _reviews.GetListAsync(
            predicate: r =>
                r.ProductId == productId &&
                (!query.Rating.HasValue || r.Rating == query.Rating.Value),
            orderBy: BuildOrder(query.SortBy),
            include: q => q.Include(r => r.User).Include(r => r.Images),
            index: query.Page - 1,
            size: query.PageSize,
            enableTracking: false,
            cancellationToken: ct);

        return new Paginate<ReviewDto>
        {
            Index = page.Index,
            Size = page.Size,
            Count = page.Count,
            Pages = page.Pages,
            Items = _mapper.Map<List<ReviewDto>>(page.Items)
        };
    }

    public async Task<ReviewDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Review? review = await _reviews.GetAsync(
            r => r.Id == id,
            include: q => q.Include(r => r.User).Include(r => r.Images),
            enableTracking: false,
            cancellationToken: ct);

        if (review is null)
            throw new NotFoundException(Messages.Review.NotFound);

        return _mapper.Map<ReviewDto>(review);
    }

    public async Task<ReviewDto> CreateAsync(int productId, CreateReviewDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Review.NotAuthenticated);

        ValidationResult validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        await _rules.ProductMustExistAsync(productId, ct);
        await _rules.UserHasNotReviewedYetAsync(_currentUser.UserId, productId, ct);

        Review entity = new()
        {
            UserId = _currentUser.UserId,
            ProductId = productId,
            Rating = dto.Rating,
            Comment = dto.Comment?.Trim()
        };

        Review created = await _reviews.AddAsync(entity, ct);
        return await GetByIdAsync(created.Id, ct);
    }

    public async Task<ReviewDto> UpdateAsync(int id, UpdateReviewDto dto, CancellationToken ct = default)
    {
        ValidationResult validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        Review existing = await _rules.ReviewMustExistAsync(id, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustBeOwner(existing, _currentUser.UserId, isAdmin);

        Review? tracked = await _reviews.GetAsync(r => r.Id == id, cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(Messages.Review.NotFound);

        tracked.Rating = dto.Rating;
        tracked.Comment = dto.Comment?.Trim();
        await _reviews.UpdateAsync(tracked, ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Review existing = await _rules.ReviewMustExistAsync(id, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustBeOwner(existing, _currentUser.UserId, isAdmin);

        Review? tracked = await _reviews.GetAsync(
            r => r.Id == id,
            include: q => q.Include(r => r.Images),
            cancellationToken: ct);
        if (tracked is null) return;

        // Best-effort: clean up image files on disk.
        foreach (ReviewImage img in tracked.Images.ToList())
            await _fileStorage.DeleteAsync(img.ImageUrl, ct);

        await _reviews.DeleteAsync(tracked, permanent: false, cancellationToken: ct);
    }

    public async Task<ReviewImageDto> AddImageAsync(int reviewId, IFormFile file, CancellationToken ct = default)
    {
        Review existing = await _rules.ReviewMustExistAsync(reviewId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustBeOwner(existing, _currentUser.UserId, isAdmin);

        string url = await _fileStorage.SaveAsync(file, ImagesSubfolder, ct);

        ReviewImage entity = new()
        {
            ReviewId = reviewId,
            ImageUrl = url
        };
        ReviewImage created = await _images.AddAsync(entity, ct);
        return _mapper.Map<ReviewImageDto>(created);
    }

    public async Task<ReviewDto?> GetMineForProductAsync(int productId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return null;

        string userId = _currentUser.UserId;
        Review? mine = await _reviews.GetAsync(
            r => r.UserId == userId && r.ProductId == productId,
            include: q => q.Include(r => r.User).Include(r => r.Images),
            enableTracking: false,
            cancellationToken: ct);

        return mine is null ? null : _mapper.Map<ReviewDto>(mine);
    }

    public async Task DeleteImageAsync(int reviewId, int imageId, CancellationToken ct = default)
    {
        Review existing = await _rules.ReviewMustExistAsync(reviewId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustBeOwner(existing, _currentUser.UserId, isAdmin);

        ReviewImage? image = await _images.GetAsync(
            i => i.Id == imageId && i.ReviewId == reviewId,
            cancellationToken: ct);
        if (image is null)
            throw new NotFoundException(Messages.Review.ImageNotFound);

        await _fileStorage.DeleteAsync(image.ImageUrl, ct);
        await _images.DeleteAsync(image, permanent: true, cancellationToken: ct);
    }

    private static Func<IQueryable<Review>, IOrderedQueryable<Review>> BuildOrder(string? sortBy) =>
        sortBy switch
        {
            "oldest" => q => q.OrderBy(r => r.CreatedDate),
            "rating-asc" => q => q.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedDate),
            "rating-desc" => q => q.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedDate),
            _ => q => q.OrderByDescending(r => r.CreatedDate)
        };

    public async Task<List<AdminReviewRowDto>> GetAllForAdminAsync(int? maxRating = null, string? search = null, int take = 100, CancellationToken ct = default)
    {
        // maxRating semantics: "show me reviews with rating ≤ N" — lets admins
        // surface low-star comments for moderation review.
        Paginate<Review> page = await _reviews.GetListAsync(
            predicate: r =>
                (!maxRating.HasValue || r.Rating <= maxRating.Value) &&
                (string.IsNullOrEmpty(search) || (r.Comment ?? "").Contains(search)),
            orderBy: q => q.OrderByDescending(r => r.CreatedDate),
            include: q => q.Include(r => r.User).Include(r => r.Product).Include(r => r.Images),
            index: 0, size: take,
            enableTracking: false,
            cancellationToken: ct);

        return page.Items.Select(r => new AdminReviewRowDto
        {
            Review = _mapper.Map<ReviewDto>(r),
            ProductId = r.ProductId,
            ProductName = r.Product?.Name ?? string.Empty
        }).ToList();
    }

    public async Task<List<SellerReviewRowDto>> GetForCurrentSellerAsync(int? maxRating = null, int take = 100, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return new List<SellerReviewRowDto>();

        string sellerId = _currentUser.UserId;

        Paginate<Review> page = await _reviews.GetListAsync(
            predicate: r =>
                r.Product.Store.OwnerId == sellerId &&
                (!maxRating.HasValue || r.Rating <= maxRating.Value),
            orderBy: q => q.OrderByDescending(r => r.CreatedDate),
            include: q => q
                .Include(r => r.User)
                .Include(r => r.Product).ThenInclude(p => p.Store)
                .Include(r => r.Product).ThenInclude(p => p.Images),
            index: 0, size: take,
            enableTracking: false,
            cancellationToken: ct);

        return page.Items.Select(r => new SellerReviewRowDto
        {
            Review = _mapper.Map<ReviewDto>(r),
            ProductId = r.ProductId,
            ProductName = r.Product?.Name ?? string.Empty,
            ProductSlug = r.Product?.Slug,
            ProductImageUrl = r.Product?.Images?
                .OrderBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .FirstOrDefault(),
            StoreId = r.Product?.StoreId ?? 0,
            StoreName = r.Product?.Store?.Name ?? string.Empty
        }).ToList();
    }
}
