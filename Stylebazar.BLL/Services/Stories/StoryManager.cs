using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Stories;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Rules;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.BLL.Services.FileStorage;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Stories;

public class StoryManager : IStoryService
{
    private const string Subfolder = "stories";
    private const int DefaultHoursToExpire = 24;

    private readonly IStoryRepository _stories;
    private readonly IStoreRepository _stores;
    private readonly IMapper _mapper;
    private readonly StoryBusinessRules _rules;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<CreateStoryDto> _createValidator;

    public StoryManager(
        IStoryRepository stories,
        IStoreRepository stores,
        IMapper mapper,
        StoryBusinessRules rules,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        IValidator<CreateStoryDto> createValidator)
    {
        _stories = stories;
        _stores = stores;
        _mapper = mapper;
        _rules = rules;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _createValidator = createValidator;
    }

    public async Task<List<StoreStoriesDto>> GetActiveFeedAsync(CancellationToken ct = default)
    {
        DateTime now = DateTime.UtcNow;

        Paginate<Story> page = await _stories.GetListAsync(
            predicate: s => s.IsActive && s.ExpiresAt > now,
            orderBy: q => q.OrderByDescending(s => s.CreatedDate),
            include: q => q.Include(s => s.Store),
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);

        return page.Items
            .Where(s => s.Store != null && s.Store.IsActive)
            .GroupBy(s => s.StoreId)
            .Select(g =>
            {
                Story first = g.First();
                return new StoreStoriesDto
                {
                    StoreId = g.Key,
                    StoreName = first.Store?.Name ?? string.Empty,
                    StoreSlug = first.Store?.Slug ?? string.Empty,
                    StoreProfileImageUrl = first.Store?.ProfileImageUrl,
                    Stories = g.OrderBy(s => s.CreatedDate).Select(s => _mapper.Map<StoryDto>(s)).ToList()
                };
            })
            .OrderByDescending(g => g.Stories.Max(s => s.CreatedDate))
            .ToList();
    }

    public async Task<List<StoryDto>> GetByStoreAsync(int storeId, bool activeOnly = true, CancellationToken ct = default)
    {
        DateTime now = DateTime.UtcNow;

        Paginate<Story> page = await _stories.GetListAsync(
            predicate: s => s.StoreId == storeId && (!activeOnly || (s.IsActive && s.ExpiresAt > now)),
            orderBy: q => q.OrderBy(s => s.CreatedDate),
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);

        return _mapper.Map<List<StoryDto>>(page.Items);
    }

    public async Task<StoryDto> CreateAsync(int storeId, CreateStoryDto dto, IFormFile file, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Story.NotAuthenticated);

        ValidationResult validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        Store store = await _rules.StoreMustExistAndBeActiveAsync(storeId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustBeOwner(store, _currentUser.UserId, isAdmin);

        await _rules.EnsureUnderActiveLimitAsync(storeId, ct);

        string url = await _fileStorage.SaveAsync(file, Subfolder, ct);

        int hours = dto.HoursToExpire ?? DefaultHoursToExpire;
        Story entity = new()
        {
            StoreId = storeId,
            ImageUrl = url,
            Caption = dto.Caption?.Trim(),
            LinkUrl = dto.LinkUrl?.Trim(),
            ExpiresAt = DateTime.UtcNow.AddHours(hours),
            IsActive = true,
            ViewCount = 0
        };

        Story created = await _stories.AddAsync(entity, ct);
        return _mapper.Map<StoryDto>(created);
    }

    public async Task<StoryDto> RecordViewAsync(int id, CancellationToken ct = default)
    {
        Story? tracked = await _stories.GetAsync(s => s.Id == id, cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(Messages.Story.NotFound);

        tracked.ViewCount += 1;
        await _stories.UpdateAsync(tracked, ct);
        return _mapper.Map<StoryDto>(tracked);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Story existing = await _rules.StoryMustExistAsync(id, ct);
        Store store = await _rules.StoreMustExistAndBeActiveAsync(existing.StoreId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustBeOwner(store, _currentUser.UserId, isAdmin);

        Story? tracked = await _stories.GetAsync(s => s.Id == id, cancellationToken: ct);
        if (tracked is null) return;

        await _fileStorage.DeleteAsync(tracked.ImageUrl, ct);
        await _stories.DeleteAsync(tracked, permanent: true, cancellationToken: ct);
    }

    public async Task<List<AdminStoryRowDto>> GetAllForAdminAsync(int take = 100, CancellationToken ct = default)
    {
        Paginate<Story> page = await _stories.GetListAsync(
            predicate: null,
            orderBy: q => q.OrderByDescending(s => s.CreatedDate),
            include: q => q.Include(s => s.Store),
            index: 0, size: take,
            enableTracking: false,
            cancellationToken: ct);

        return page.Items.Select(s => new AdminStoryRowDto
        {
            Story = _mapper.Map<StoryDto>(s),
            StoreName = s.Store?.Name ?? string.Empty,
            StoreSlug = s.Store?.Slug ?? string.Empty
        }).ToList();
    }
}
