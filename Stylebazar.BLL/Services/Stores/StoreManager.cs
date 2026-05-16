using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Helpers;
using Stylebazar.BLL.Rules;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.BLL.Services.FileStorage;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Stores;

public class StoreManager : IStoreService
{
    private const string ProfileSubfolder = "stores/profile";
    private const string CoverSubfolder = "stores/cover";

    private readonly IStoreRepository _repo;
    private readonly IMapper _mapper;
    private readonly StoreBusinessRules _rules;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _files;
    private readonly IValidator<CreateStoreDto> _createValidator;
    private readonly IValidator<UpdateStoreDto> _updateValidator;

    public StoreManager(
        IStoreRepository repo,
        IMapper mapper,
        StoreBusinessRules rules,
        ICurrentUserService currentUser,
        IFileStorageService files,
        IValidator<CreateStoreDto> createValidator,
        IValidator<UpdateStoreDto> updateValidator)
    {
        _repo = repo;
        _mapper = mapper;
        _rules = rules;
        _currentUser = currentUser;
        _files = files;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<Paginate<StoreListDto>> GetListAsync(StoreListQueryDto query, CancellationToken ct = default)
    {
        Paginate<Store> page = await _repo.GetListAsync(
            predicate: s =>
                (!query.IsActive.HasValue || s.IsActive == query.IsActive.Value) &&
                (string.IsNullOrEmpty(query.OwnerId) || s.OwnerId == query.OwnerId) &&
                (string.IsNullOrEmpty(query.Search) || s.Name.Contains(query.Search)),
            orderBy: q => q.OrderByDescending(s => s.CreatedDate),
            // Products.Images is included so the shop-card side-rail can render
            // up to 5 thumbnails per store without an N+1 lookup.
            include: q => q.Include(s => s.Products).ThenInclude(p => p.Images),
            index: query.Page - 1,
            size: query.PageSize,
            enableTracking: false,
            cancellationToken: ct);

        return new Paginate<StoreListDto>
        {
            Index = page.Index,
            Size = page.Size,
            Count = page.Count,
            Pages = page.Pages,
            Items = _mapper.Map<List<StoreListDto>>(page.Items)
        };
    }

    public async Task<List<StoreListDto>> GetMyStoresAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Store.OwnerNotAuthenticated);

        Paginate<Store> page = await _repo.GetListAsync(
            predicate: s => s.OwnerId == _currentUser.UserId,
            orderBy: q => q.OrderByDescending(s => s.CreatedDate),
            include: q => q.Include(s => s.Products),
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);

        return _mapper.Map<List<StoreListDto>>(page.Items);
    }

    public async Task<StoreDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Store store = await GetWithDetailsAsync(s => s.Id == id, ct);
        return _mapper.Map<StoreDetailDto>(store);
    }

    public async Task<StoreDetailDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        Store store = await GetWithDetailsAsync(s => s.Slug == slug, ct);
        return _mapper.Map<StoreDetailDto>(store);
    }

    public async Task<StoreDetailDto> CreateAsync(CreateStoreDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Store.OwnerNotAuthenticated);

        ValidationResult validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        string slug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugHelper.Slugify(dto.Name) : dto.Slug.Trim();
        await _rules.SlugMustBeUniqueAsync(slug, excludeId: null, ct: ct);

        Store entity = new()
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Description = dto.Description?.Trim(),
            ProfileImageUrl = dto.ProfileImageUrl?.Trim() ?? string.Empty,
            CoverImageUrl = dto.CoverImageUrl?.Trim(),
            ContactPhone = dto.ContactPhone?.Trim(),
            Address = dto.Address?.Trim(),
            OwnerId = _currentUser.UserId!,
            IsActive = true
        };

        Store created = await _repo.AddAsync(entity, ct);
        return await GetByIdAsync(created.Id, ct);
    }

    public async Task<StoreDetailDto> UpdateAsync(int id, UpdateStoreDto dto, CancellationToken ct = default)
    {
        ValidationResult validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        Store existing = await _rules.StoreMustExistAsync(id, ct);

        // Only the owner can update — admins can override via separate endpoints if needed.
        bool isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin)
            _rules.MustBeOwner(existing, _currentUser.UserId);

        string newSlug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugHelper.Slugify(dto.Name) : dto.Slug.Trim();
        await _rules.SlugMustBeUniqueAsync(newSlug, excludeId: id, ct: ct);

        Store? tracked = await _repo.GetAsync(s => s.Id == id, cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(Messages.Store.NotFound);

        tracked.Name = dto.Name.Trim();
        tracked.Slug = newSlug;
        tracked.Description = NullIfBlank(dto.Description);
        // ProfileImageUrl + CoverImageUrl are managed by the dedicated image
        // upload endpoints — UpdateAsync only changes them if the caller
        // explicitly provides a non-empty value (lets the form post safely
        // omit the field without wiping the saved image).
        if (!string.IsNullOrWhiteSpace(dto.ProfileImageUrl))
            tracked.ProfileImageUrl = dto.ProfileImageUrl.Trim();
        if (dto.CoverImageUrl is not null)
            tracked.CoverImageUrl = NullIfBlank(dto.CoverImageUrl);
        tracked.ContactPhone = NullIfBlank(dto.ContactPhone);
        tracked.Address = NullIfBlank(dto.Address);
        tracked.WhatsAppNumber = NullIfBlank(dto.WhatsAppNumber);
        tracked.InstagramHandle = NullIfBlank(dto.InstagramHandle);
        tracked.TelegramHandle = NullIfBlank(dto.TelegramHandle);
        tracked.FacebookHandle = NullIfBlank(dto.FacebookHandle);
        tracked.TikTokHandle = NullIfBlank(dto.TikTokHandle);

        await _repo.UpdateAsync(tracked, ct);
        return await GetByIdAsync(id, ct);
    }

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Store existing = await _rules.StoreMustExistAsync(id, ct);

        bool isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin)
            _rules.MustBeOwner(existing, _currentUser.UserId);

        await _rules.CannotDeleteWhenHasProductsAsync(id, ct);

        Store? tracked = await _repo.GetAsync(s => s.Id == id, cancellationToken: ct);
        if (tracked is null) return;

        await _repo.DeleteAsync(tracked, permanent: false, cancellationToken: ct);
    }

    public async Task<StoreDetailDto> ToggleActiveAsync(int id, CancellationToken ct = default)
    {
        Store existing = await _rules.StoreMustExistAsync(id, ct);

        Store? tracked = await _repo.GetAsync(s => s.Id == id, cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(Messages.Store.NotFound);

        // Owner guard — Admins can override.
        bool isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin)
            _rules.MustBeOwner(existing, _currentUser.UserId);

        tracked.IsActive = !tracked.IsActive;
        await _repo.UpdateAsync(tracked, ct);
        return await GetByIdAsync(id, ct);
    }

    // ---------- Image uploads ----------

    public async Task<string> UploadProfileImageAsync(int storeId, IFormFile file, CancellationToken ct = default)
    {
        Store tracked = await RequireOwnedStoreAsync(storeId, ct);

        string? previous = string.IsNullOrEmpty(tracked.ProfileImageUrl) ? null : tracked.ProfileImageUrl;
        string url = await _files.SaveAsync(file, ProfileSubfolder, ct);

        tracked.ProfileImageUrl = url;
        await _repo.UpdateAsync(tracked, ct);

        // Best-effort delete of the prior image — never blocks the upload.
        if (!string.IsNullOrEmpty(previous))
            await _files.DeleteAsync(previous, ct);

        return url;
    }

    public async Task<string> UploadCoverImageAsync(int storeId, IFormFile file, CancellationToken ct = default)
    {
        Store tracked = await RequireOwnedStoreAsync(storeId, ct);

        string? previous = tracked.CoverImageUrl;
        string url = await _files.SaveAsync(file, CoverSubfolder, ct);

        tracked.CoverImageUrl = url;
        await _repo.UpdateAsync(tracked, ct);

        if (!string.IsNullOrEmpty(previous))
            await _files.DeleteAsync(previous, ct);

        return url;
    }

    public async Task RemoveCoverImageAsync(int storeId, CancellationToken ct = default)
    {
        Store tracked = await RequireOwnedStoreAsync(storeId, ct);

        string? previous = tracked.CoverImageUrl;
        if (string.IsNullOrEmpty(previous)) return;

        tracked.CoverImageUrl = null;
        await _repo.UpdateAsync(tracked, ct);
        await _files.DeleteAsync(previous, ct);
    }

    private async Task<Store> RequireOwnedStoreAsync(int storeId, CancellationToken ct)
    {
        Store? tracked = await _repo.GetAsync(s => s.Id == storeId, cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(Messages.Store.NotFound);

        bool isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin)
            _rules.MustBeOwner(tracked, _currentUser.UserId);

        return tracked;
    }

    private async Task<Store> GetWithDetailsAsync(System.Linq.Expressions.Expression<Func<Store, bool>> predicate, CancellationToken ct)
    {
        Store? store = await _repo.GetAsync(
            predicate,
            include: q => q
                .Include(s => s.Owner)
                .Include(s => s.Products),
            enableTracking: false,
            cancellationToken: ct);

        if (store is null)
            throw new NotFoundException(Messages.Store.NotFound);

        return store;
    }
}
