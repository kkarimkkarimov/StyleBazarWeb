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
using Stylebazar.BLL.Services.Subscriptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Products;

public class ProductManager : IProductService
{
    private readonly IProductRepository _products;
    private readonly IProductImageRepository _images;
    private readonly IProductSizeRepository _sizes;
    private readonly IProductColorRepository _colors;
    private readonly IStoreRepository _stores;
    private readonly IMapper _mapper;
    private readonly ProductBusinessRules _rules;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly ISubscriptionService _subscriptions;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;

    private const string ImagesSubfolder = "products";

    public ProductManager(
        IProductRepository products,
        IProductImageRepository images,
        IProductSizeRepository sizes,
        IProductColorRepository colors,
        IStoreRepository stores,
        IMapper mapper,
        ProductBusinessRules rules,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        ISubscriptionService subscriptions,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator)
    {
        _products = products;
        _images = images;
        _sizes = sizes;
        _colors = colors;
        _stores = stores;
        _mapper = mapper;
        _rules = rules;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _subscriptions = subscriptions;
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
                (!query.InStock.HasValue || (query.InStock.Value ? p.StockQuantity > 0 : true)) &&
                (!query.Condition.HasValue || p.Condition == query.Condition.Value),
            orderBy: BuildOrder(query.SortBy),
            include: q => q.Include(p => p.Store).Include(p => p.Category).Include(p => p.Images).Include(p => p.Reviews),
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
            include: q => q.Include(p => p.Store).Include(p => p.Category).Include(p => p.Images).Include(p => p.Reviews),
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
            include: q => q.Include(p => p.Store).Include(p => p.Category).Include(p => p.Images).Include(p => p.Reviews),
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);

        return _mapper.Map<List<ProductListDto>>(page.Items);
    }

    public async Task<List<ProductListDto>> GetMineAsync(bool? isActive = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return new List<ProductListDto>();

        string userId = _currentUser.UserId;

        Paginate<Product> page = await _products.GetListAsync(
            predicate: p => p.Store.OwnerId == userId
                            && (!isActive.HasValue || p.IsActive == isActive.Value),
            orderBy: q => q.OrderByDescending(p => p.CreatedDate),
            include: q => q.Include(p => p.Store).Include(p => p.Category).Include(p => p.Images).Include(p => p.Reviews),
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

        // ---- Plan quota gate ----
        // Always scoped to the STORE OWNER, not the signed-in user. An
        // admin creating a product on behalf of a seller still consumes
        // that seller's quota slot. Admin can lift the cap by upgrading
        // the seller's plan first (Admin/User/Detail → Plan dəyiş).
        QuotaCheckResult quota = await _subscriptions.CanUserAddProductAsync(store.OwnerId, ct);
        if (!quota.Ok)
            throw new BusinessException(quota.ReasonMessage ?? "Plan limiti aşılıb.");

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
            Brand = NullIfBlank(dto.Brand),
            Condition = dto.Condition,
            StoreId = dto.StoreId,
            CategoryId = dto.CategoryId,
            IsActive = true,
            IsFeatured = false,
            HasDelivery = dto.HasDelivery,
            Material = NullIfBlank(dto.Material),
            Pattern = NullIfBlank(dto.Pattern),
            Neckline = NullIfBlank(dto.Neckline),
            SleeveLength = NullIfBlank(dto.SleeveLength),
            Fit = NullIfBlank(dto.Fit),
            Season = NullIfBlank(dto.Season),
            Style = NullIfBlank(dto.Style),
            CountryOfOrigin = NullIfBlank(dto.CountryOfOrigin),
            Sizes = dto.Sizes.Select(s => new ProductSize
            {
                Size = s.Size.Trim(),
                StockQuantity = s.StockQuantity
            }).ToList(),
            Colors = dto.Colors.Select((c, i) => new ProductColor
            {
                Name = c.Name.Trim(),
                Hex = c.Hex.Trim(),
                DisplayOrder = i
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

        Product? tracked = await _products.GetAsync(
            p => p.Id == id,
            include: q => q.Include(p => p.Sizes).Include(p => p.Colors),
            cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(Messages.Product.NotFound);

        Store store = await _rules.StoreMustExistAndBeActiveAsync(tracked.StoreId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustOwnStore(store, _currentUser.UserId, isAdmin);

        await _rules.CategoryMustExistAsync(dto.CategoryId, ct);
        _rules.DiscountMustBeLowerThanPrice(dto.Price, dto.DiscountPrice);

        string newSlug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugHelper.Slugify(dto.Name) : dto.Slug.Trim();
        await _rules.SlugMustBeUniqueAsync(newSlug, excludeId: id, ct: ct);

        // ---- core fields ----
        tracked.Name = dto.Name.Trim();
        tracked.Slug = newSlug;
        tracked.Description = dto.Description.Trim();
        tracked.Price = dto.Price;
        tracked.DiscountPrice = dto.DiscountPrice;
        tracked.StockQuantity = dto.StockQuantity;
        tracked.Brand = NullIfBlank(dto.Brand);
        tracked.Condition = dto.Condition;
        tracked.CategoryId = dto.CategoryId;
        tracked.IsActive = dto.IsActive;
        tracked.HasDelivery = dto.HasDelivery;
        tracked.Material = NullIfBlank(dto.Material);
        tracked.Pattern = NullIfBlank(dto.Pattern);
        tracked.Neckline = NullIfBlank(dto.Neckline);
        tracked.SleeveLength = NullIfBlank(dto.SleeveLength);
        tracked.Fit = NullIfBlank(dto.Fit);
        tracked.Season = NullIfBlank(dto.Season);
        tracked.Style = NullIfBlank(dto.Style);
        tracked.CountryOfOrigin = NullIfBlank(dto.CountryOfOrigin);

        await _products.UpdateAsync(tracked, ct);

        // ---- sizes diff ----
        // Replace-all semantics: tags the user removed get deleted, new tags
        // are inserted, kept tags update their stock without losing their Id
        // (so any inbound references — none today, but future-proof — survive).
        await SyncSizesAsync(tracked.Id, tracked.Sizes.ToList(), dto.Sizes, ct);

        // ---- colors diff ----
        await SyncColorsAsync(tracked.Id, tracked.Colors.ToList(), dto.Colors, ct);

        return await GetByIdAsync(id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Product? tracked = await _products.GetAsync(
            p => p.Id == id,
            include: q => q.Include(p => p.Images),
            cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(Messages.Product.NotFound);

        Store store = await _rules.StoreMustExistAndBeActiveAsync(tracked.StoreId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustOwnStore(store, _currentUser.UserId, isAdmin);

        // Soft delete the product. Image files stay on disk; permanent delete should
        // sweep them, but soft delete leaves data recoverable.
        await _products.DeleteAsync(tracked, permanent: false, cancellationToken: ct);
    }

    public async Task<ProductDetailDto> ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        Product? tracked = await _products.GetAsync(p => p.Id == id, cancellationToken: ct);
        if (tracked is null) throw new NotFoundException(Messages.Product.NotFound);

        Store store = await _rules.StoreMustExistAndBeActiveAsync(tracked.StoreId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustOwnStore(store, _currentUser.UserId, isAdmin);

        tracked.IsActive = !tracked.IsActive;
        await _products.UpdateAsync(tracked, ct);
        return await GetByIdAsync(id, ct);
    }

    // ---------- Images ----------

    public async Task<ProductImageDto> AddImageAsync(int productId, IFormFile file, bool isMain, CancellationToken ct = default)
    {
        Product product = await EnsureOwnedProductAsync(productId, ct);

        string url = await _fileStorage.SaveAsync(file, ImagesSubfolder, ct);

        // If flagged as main OR this is the very first image, ensure exactly
        // one main exists. First-image auto-promotion saves the seller a click.
        int existingCount = await CountImagesAsync(productId, ct);
        bool shouldBeMain = isMain || existingCount == 0;

        if (shouldBeMain)
        {
            Paginate<ProductImage> mains = await _images.GetListAsync(
                predicate: i => i.ProductId == productId && i.IsMain,
                orderBy: q => q.OrderBy(i => i.Id),
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
            IsMain = shouldBeMain,
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

        bool wasMain = image.IsMain;

        // Best-effort delete on disk; database delete is the source of truth.
        await _fileStorage.DeleteAsync(image.ImageUrl, ct);
        await _images.DeleteAsync(image, permanent: true, cancellationToken: ct);

        // Promote the next image to main so the PDP card never shows a fallback.
        if (wasMain)
        {
            Paginate<ProductImage> next = await _images.GetListAsync(
                predicate: i => i.ProductId == productId,
                orderBy: q => q.OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id),
                index: 0, size: 1,
                cancellationToken: ct);
            ProductImage? successor = next.Items.FirstOrDefault();
            if (successor is not null)
            {
                successor.IsMain = true;
                await _images.UpdateAsync(successor, ct);
            }
        }
    }

    public async Task<ProductImageDto> SetMainImageAsync(int productId, int imageId, CancellationToken ct = default)
    {
        Product product = await EnsureOwnedProductAsync(productId, ct);

        Paginate<ProductImage> all = await _images.GetListAsync(
            predicate: i => i.ProductId == productId,
            orderBy: q => q.OrderBy(i => i.DisplayOrder),
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
            throw new NotFoundException(Messages.Product.NotFound);

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
            throw new NotFoundException(Messages.Product.NotFound);

        Store store = await _rules.StoreMustExistAndBeActiveAsync(product.StoreId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustOwnStore(store, _currentUser.UserId, isAdmin);
        return product;
    }

    private async Task<int> CountImagesAsync(int productId, CancellationToken ct)
    {
        Paginate<ProductImage> page = await _images.GetListAsync(
            predicate: i => i.ProductId == productId,
            orderBy: q => q.OrderBy(i => i.Id),
            index: 0, size: 1,
            enableTracking: false,
            cancellationToken: ct);
        return page.Count;
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
                .Include(p => p.Colors)
                .Include(p => p.Reviews),
            enableTracking: false,
            cancellationToken: ct);

        if (product is null)
            throw new NotFoundException(Messages.Product.NotFound);

        return product;
    }

    /// <summary>
    /// Diff existing sizes against the incoming list. Keeps rows with matching
    /// labels (updates their stock), deletes the rest, adds new ones.
    /// </summary>
    private async Task SyncSizesAsync(int productId, List<ProductSize> existing, List<CreateProductSizeDto> incoming, CancellationToken ct)
    {
        // Normalise inbound — keep only the last entry for duplicate labels
        var byLabel = new Dictionary<string, CreateProductSizeDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in incoming)
        {
            string label = (s.Size ?? string.Empty).Trim();
            if (label.Length == 0) continue;
            byLabel[label] = s;
        }

        // Delete rows whose label is no longer in the inbound set
        foreach (ProductSize row in existing)
        {
            if (!byLabel.ContainsKey(row.Size))
                await _sizes.DeleteAsync(row, permanent: true, cancellationToken: ct);
        }

        // Upsert: update stock for existing labels, add new
        var existingByLabel = existing.ToDictionary(r => r.Size, r => r, StringComparer.OrdinalIgnoreCase);
        foreach (var (label, dto) in byLabel)
        {
            if (existingByLabel.TryGetValue(label, out ProductSize? row))
            {
                if (row.StockQuantity != dto.StockQuantity)
                {
                    row.StockQuantity = dto.StockQuantity;
                    await _sizes.UpdateAsync(row, ct);
                }
            }
            else
            {
                await _sizes.AddAsync(new ProductSize
                {
                    ProductId = productId,
                    Size = label,
                    StockQuantity = dto.StockQuantity
                }, ct);
            }
        }
    }

    /// <summary>
    /// Same shape as <see cref="SyncSizesAsync"/> but for colours — matched by
    /// case-insensitive name. Hex code updates in place; new names appended at end.
    /// </summary>
    private async Task SyncColorsAsync(int productId, List<ProductColor> existing, List<CreateProductColorDto> incoming, CancellationToken ct)
    {
        var byName = new Dictionary<string, CreateProductColorDto>(StringComparer.OrdinalIgnoreCase);
        int order = 0;
        var orderMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in incoming)
        {
            string name = (c.Name ?? string.Empty).Trim();
            if (name.Length == 0) continue;
            byName[name] = c;
            orderMap[name] = order++;
        }

        foreach (ProductColor row in existing)
        {
            if (!byName.ContainsKey(row.Name))
                await _colors.DeleteAsync(row, permanent: true, cancellationToken: ct);
        }

        var existingByName = existing.ToDictionary(r => r.Name, r => r, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, dto) in byName)
        {
            string hex = dto.Hex.Trim();
            int desiredOrder = orderMap[name];

            if (existingByName.TryGetValue(name, out ProductColor? row))
            {
                bool changed = row.Hex != hex || row.DisplayOrder != desiredOrder;
                if (changed)
                {
                    row.Hex = hex;
                    row.DisplayOrder = desiredOrder;
                    await _colors.UpdateAsync(row, ct);
                }
            }
            else
            {
                await _colors.AddAsync(new ProductColor
                {
                    ProductId = productId,
                    Name = name,
                    Hex = hex,
                    DisplayOrder = desiredOrder
                }, ct);
            }
        }
    }

    private static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // Sort strategy:
    //   - Explicit user-driven sorts (price/name) honor the user choice
    //     verbatim — they paid for a specific axis, don't reshuffle.
    //   - The default ("newest"/no filter) applies a SUBTLE catalog
    //     priority: VIP first, then Pro, then Free, with CreatedDate
    //     as the tiebreaker inside each tier. This rewards paying
    //     sellers without burying free listings — a Free product from
    //     today still beats a VIP product from a month ago because
    //     freshness wins the secondary sort.
    private static Func<IQueryable<Product>, IOrderedQueryable<Product>> BuildOrder(string? sortBy) =>
        sortBy switch
        {
            "price-asc" => q => q.OrderBy(p => p.DiscountPrice ?? p.Price),
            "price-desc" => q => q.OrderByDescending(p => p.DiscountPrice ?? p.Price),
            "name-asc" => q => q.OrderBy(p => p.Name),
            "name-desc" => q => q.OrderByDescending(p => p.Name),
            _ => q => q
                .OrderByDescending(p => (int)p.Store.Owner.CurrentPlan)
                .ThenByDescending(p => p.CreatedDate)
        };
}
