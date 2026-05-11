using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Helpers;
using Stylebazar.BLL.Rules;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.BLL.Services.FileStorage;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Products;

public class ProductManager : IProductService
{
    private readonly IProductRepository _products;
    private readonly IProductImageRepository _images;
    private readonly IStoreRepository _stores;
    private readonly IMapper _mapper;
    private readonly ProductBusinessRules _rules;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;

    private const string ImagesSubfolder = "products";

    public ProductManager(
        IProductRepository products,
        IProductImageRepository images,
        IStoreRepository stores,
        IMapper mapper,
        ProductBusinessRules rules,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator)
    {
        _products = products;
        _images = images;
        _stores = stores;
        _mapper = mapper;
        _rules = rules;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    // ---------- Read ----------

    public async Task<Paginate<ProductListDto>> GetListAsync(ProductListQueryDto query, CancellationToken ct = default)
    {
        Paginate<Product> page = await _products.GetListAsync(
            predicate: p =>
                (string.IsNullOrEmpty(query.Search) || p.Name.Contains(query.Search)) &&
                (!query.CategoryId.HasValue || p.CategoryId == query.CategoryId.Value) &&
                (!query.StoreId.HasValue || p.StoreId == query.StoreId.Value) &&
                (string.IsNullOrEmpty(query.Brand) || p.Brand == query.Brand) &&
                (!query.MinPrice.HasValue || (p.DiscountPrice ?? p.Price) >= query.MinPrice.Value) &&
                (!query.MaxPrice.HasValue || (p.DiscountPrice ?? p.Price) <= query.MaxPrice.Value) &&
                (!query.IsFeatured.HasValue ||
                    (query.IsFeatured.Value
                        ? p.IsFeatured && (p.FeaturedUntil == null || p.FeaturedUntil > DateTime.UtcNow)
                        : !p.IsFeatured)) &&
                (!query.IsActive.HasValue || p.IsActive == query.IsActive.Value) &&
                (!query.Condition.HasValue || p.Condition == query.Condition.Value),
            orderBy: BuildOrder(query.SortBy),
            include: q => q.Include(p => p.Store).Include(p => p.Category).Include(p => p.Images),
            index: query.Page - 1,
            size: query.PageSize,
            enableTracking: false,
            cancellationToken: ct);

        return new Paginate<ProductListDto>
        {
            Index = page.Index,
            Size = page.Size,
            Count = page.Count,
            Pages = page.Pages,
            Items = _mapper.Map<List<ProductListDto>>(page.Items)
        };
    }

    public async Task<List<ProductListDto>> GetFeaturedAsync(int take = 12, CancellationToken ct = default)
    {
        if (take < 1) take = 1;
        if (take > 50) take = 50;

        Paginate<Product> page = await _products.GetListAsync(
            predicate: p => p.IsActive && p.IsFeatured && (p.FeaturedUntil == null || p.FeaturedUntil > DateTime.UtcNow),
            orderBy: q => q.OrderByDescending(p => p.CreatedDate),
            include: q => q.Include(p => p.Store).Include(p => p.Category).Include(p => p.Images),
            index: 0,
            size: take,
            enableTracking: false,
            cancellationToken: ct);

        return _mapper.Map<List<ProductListDto>>(page.Items);
    }

    public async Task<List<ProductListDto>> GetByStoreAsync(int storeId, CancellationToken ct = default)
    {
        Paginate<Product> page = await _products.GetListAsync(
            predicate: p => p.StoreId == storeId,
            orderBy: q => q.OrderByDescending(p => p.CreatedDate),
            include: q => q.Include(p => p.Store).Include(p => p.Category).Include(p => p.Images),
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);

        return _mapper.Map<List<ProductListDto>>(page.Items);
    }

    public async Task<ProductDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Product product = await GetWithDetailsAsync(p => p.Id == id, ct);
        return _mapper.Map<ProductDetailDto>(product);
    }

    public async Task<ProductDetailDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        Product product = await GetWithDetailsAsync(p => p.Slug == slug, ct);
        return _mapper.Map<ProductDetailDto>(product);
    }

    // ---------- Write ----------

    public async Task<ProductDetailDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        ValidationResult validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        Store store = await _rules.StoreMustExistAndBeActiveAsync(dto.StoreId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustOwnStore(store, _currentUser.UserId, isAdmin);

        await _rules.CategoryMustExistAsync(dto.CategoryId, ct);
        _rules.DiscountMustBeLowerThanPrice(dto.Price, dto.DiscountPrice);

        string slug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugHelper.Slugify(dto.Name) : dto.Slug.Trim();
        await _rules.SlugMustBeUniqueAsync(slug, excludeId: null, ct: ct);

        Product entity = new()
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Description = dto.Description.Trim(),
            Price = dto.Price,
            DiscountPrice = dto.DiscountPrice,
            StockQuantity = dto.StockQuantity,
            Brand = dto.Brand?.Trim(),
            Condition = dto.Condition,
            StoreId = dto.StoreId,
            CategoryId = dto.CategoryId,
            IsActive = true,
            IsFeatured = false,
            Sizes = dto.Sizes.Select(s => new ProductSize
            {
                Size = s.Size.Trim(),
                StockQuantity = s.StockQuantity
            }).ToList()
        };

        Product created = await _products.AddAsync(entity, ct);
        return await GetByIdAsync(created.Id, ct);
    }

    public async Task<ProductDetailDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default)
    {
        ValidationResult validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        Product? tracked = await _products.GetAsync(p => p.Id == id, cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(nameof(Product), id);

        Store store = await _rules.StoreMustExistAndBeActiveAsync(tracked.StoreId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustOwnStore(store, _currentUser.UserId, isAdmin);

        await _rules.CategoryMustExistAsync(dto.CategoryId, ct);
        _rules.DiscountMustBeLowerThanPrice(dto.Price, dto.DiscountPrice);

        string newSlug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugHelper.Slugify(dto.Name) : dto.Slug.Trim();
        await _rules.SlugMustBeUniqueAsync(newSlug, excludeId: id, ct: ct);

        tracked.Name = dto.Name.Trim();
        tracked.Slug = newSlug;
        tracked.Description = dto.Description.Trim();
        tracked.Price = dto.Price;
        tracked.DiscountPrice = dto.DiscountPrice;
        tracked.StockQuantity = dto.StockQuantity;
        tracked.Brand = dto.Brand?.Trim();
        tracked.Condition = dto.Condition;
        tracked.CategoryId = dto.CategoryId;
        tracked.IsActive = dto.IsActive;

        await _products.UpdateAsync(tracked, ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Product? tracked = await _products.GetAsync(
            p => p.Id == id,
            include: q => q.Include(p => p.Images),
            cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(nameof(Product), id);

        Store store = await _rules.StoreMustExistAndBeActiveAsync(tracked.StoreId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustOwnStore(store, _currentUser.UserId, isAdmin);

        // Soft delete the product. Image files stay on disk; permanent delete should
        // sweep them, but soft delete leaves data recoverable.
        await _products.DeleteAsync(tracked, permanent: false, cancellationToken: ct);
    }

    // ---------- Images ----------

    public async Task<ProductImageDto> AddImageAsync(int productId, IFormFile file, bool isMain, CancellationToken ct = default)
    {
        Product product = await EnsureOwnedProductAsync(productId, ct);

        string url = await _fileStorage.SaveAsync(file, ImagesSubfolder, ct);

        // If flagged as main, demote any existing main image first.
        if (isMain)
        {
            Paginate<ProductImage> mains = await _images.GetListAsync(
                predicate: i => i.ProductId == productId && i.IsMain,
                index: 0,
                size: int.MaxValue,
                cancellationToken: ct);
            foreach (ProductImage m in mains.Items)
            {
                m.IsMain = false;
                await _images.UpdateAsync(m, ct);
            }
        }

        int displayOrder = await NextDisplayOrderAsync(productId, ct);

        ProductImage entity = new()
        {
            ProductId = productId,
            ImageUrl = url,
            IsMain = isMain,
            DisplayOrder = displayOrder
        };
        ProductImage created = await _images.AddAsync(entity, ct);
        return _mapper.Map<ProductImageDto>(created);
    }

    public async Task DeleteImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        Product product = await EnsureOwnedProductAsync(productId, ct);

        ProductImage? image = await _images.GetAsync(
            i => i.Id == imageId && i.ProductId == productId,
            cancellationToken: ct);
        if (image is null)
            throw new NotFoundException(Messages.Product.ImageNotFound);

        // Best-effort delete on disk; database delete is the source of truth.
        await _fileStorage.DeleteAsync(image.ImageUrl, ct);
        await _images.DeleteAsync(image, permanent: true, cancellationToken: ct);
    }

    public async Task<ProductImageDto> SetMainImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        Product product = await EnsureOwnedProductAsync(productId, ct);

        Paginate<ProductImage> all = await _images.GetListAsync(
            predicate: i => i.ProductId == productId,
            index: 0,
            size: int.MaxValue,
            cancellationToken: ct);

        ProductImage? target = all.Items.FirstOrDefault(i => i.Id == imageId);
        if (target is null)
            throw new NotFoundException(Messages.Product.ImageNotFound);

        foreach (ProductImage img in all.Items)
        {
            bool shouldBeMain = img.Id == imageId;
            if (img.IsMain != shouldBeMain)
            {
                img.IsMain = shouldBeMain;
                await _images.UpdateAsync(img, ct);
            }
        }

        return _mapper.Map<ProductImageDto>(target);
    }

    // ---------- Featured ----------

    public async Task<ProductDetailDto> SetFeaturedAsync(int productId, int? days, CancellationToken ct = default)
    {
        Product product = await EnsureOwnedProductAsync(productId, ct);

        Product? tracked = await _products.GetAsync(p => p.Id == productId, cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(nameof(Product), productId);

        tracked.IsFeatured = true;
        tracked.FeaturedUntil = days.HasValue ? DateTime.UtcNow.AddDays(days.Value) : null;
        await _products.UpdateAsync(tracked, ct);

        return await GetByIdAsync(productId, ct);
    }

    // ---------- Helpers ----------

    private async Task<Product> EnsureOwnedProductAsync(int productId, CancellationToken ct)
    {
        Product? product = await _products.GetAsync(
            p => p.Id == productId,
            enableTracking: false,
            cancellationToken: ct);
        if (product is null)
            throw new NotFoundException(nameof(Product), productId);

        Store store = await _rules.StoreMustExistAndBeActiveAsync(product.StoreId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustOwnStore(store, _currentUser.UserId, isAdmin);
        return product;
    }

    private async Task<int> NextDisplayOrderAsync(int productId, CancellationToken ct)
    {
        Paginate<ProductImage> page = await _images.GetListAsync(
            predicate: i => i.ProductId == productId,
            orderBy: q => q.OrderByDescending(i => i.DisplayOrder),
            index: 0,
            size: 1,
            enableTracking: false,
            cancellationToken: ct);

        return page.Items.Any() ? page.Items[0].DisplayOrder + 1 : 0;
    }

    private async Task<Product> GetWithDetailsAsync(System.Linq.Expressions.Expression<Func<Product, bool>> predicate, CancellationToken ct)
    {
        Product? product = await _products.GetAsync(
            predicate,
            include: q => q
                .Include(p => p.Store)
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .Include(p => p.Reviews),
            enableTracking: false,
            cancellationToken: ct);

        if (product is null)
            throw new NotFoundException(nameof(Product), 0);

        return product;
    }

    private static Func<IQueryable<Product>, IOrderedQueryable<Product>> BuildOrder(string? sortBy) =>
        sortBy switch
        {
            "price-asc" => q => q.OrderBy(p => p.DiscountPrice ?? p.Price),
            "price-desc" => q => q.OrderByDescending(p => p.DiscountPrice ?? p.Price),
            "name-asc" => q => q.OrderBy(p => p.Name),
            "name-desc" => q => q.OrderByDescending(p => p.Name),
            _ => q => q.OrderByDescending(p => p.CreatedDate)
        };
}
