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
using Stylebazar.DAL.Entities.Enums;
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
                    // Owner is the user the story-reply chat message goes to.
                    StoreOwnerId = first.Store?.OwnerId ?? string.Empty,
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

        if (file is null || file.Length == 0)
            throw new BusinessException("Hekayə üçün şəkil və ya video fayl seçməlisən.");

        ValidationResult validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        Store store = await _rules.StoreMustExistAndBeActiveAsync(storeId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustBeOwner(store, _currentUser.UserId, isAdmin);

        await _rules.EnsureUnderActiveLimitAsync(storeId, ct);

        // Detect image vs. video from the MIME type the browser sent. Defensive
        // fallback to extension because some clients omit content-type.
        StoryMediaType mediaType = DetectMediaType(file);

        // Size cap — images 10MB, videos 30MB. The validator already capped
        // duration; this protects the disk regardless.
        long maxBytes = mediaType == StoryMediaType.Video ? 30L * 1024 * 1024 : 10L * 1024 * 1024;
        if (file.Length > maxBytes)
            throw new BusinessException(mediaType == StoryMediaType.Video
                ? "Video maksimum 30 MB ola bilər."
                : "Şəkil maksimum 10 MB ola bilər.");

        // For video uploads, DurationSeconds is required (client reads it from
        // the <video> element before posting). Reject otherwise — without it we
        // can't drive the viewer's progress bar correctly.
        if (mediaType == StoryMediaType.Video && (!dto.DurationSeconds.HasValue || dto.DurationSeconds <= 0))
            throw new BusinessException("Video üçün uzunluq (saniyə) göndərilməlidir.");

        string url = await _fileStorage.SaveAsync(file, Subfolder, ct);

        int hours = dto.HoursToExpire ?? DefaultHoursToExpire;
        Story entity = new()
        {
            StoreId = storeId,
            MediaUrl = url,
            MediaType = mediaType,
            DurationSeconds = mediaType == StoryMediaType.Video ? dto.DurationSeconds : null,
            Caption = dto.Caption?.Trim(),
            LinkUrl = dto.LinkUrl?.Trim(),
            ExpiresAt = DateTime.UtcNow.AddHours(hours),
            IsActive = true,
            ViewCount = 0
        };

        Story created = await _stories.AddAsync(entity, ct);
        return _mapper.Map<StoryDto>(created);
    }

    /// <summary>
    /// MIME-based image-vs-video detection with a defensive file-extension fallback.
    /// Whitelist (image/*, video/mp4, video/webm) — anything else falls back to
    /// extension; truly unknown → image so the upload doesn't 500 (the size cap +
    /// browser content-type are the real gates).
    /// </summary>
    private static StoryMediaType DetectMediaType(IFormFile file)
    {
        string ct = (file.ContentType ?? string.Empty).ToLowerInvariant();
        if (ct.StartsWith("video/")) return StoryMediaType.Video;
        if (ct.StartsWith("image/")) return StoryMediaType.Image;

        string ext = System.IO.Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        return ext switch
        {
            ".mp4" or ".webm" or ".mov" or ".m4v" => StoryMediaType.Video,
            _ => StoryMediaType.Image
        };
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
        // Deleting a story must work even if its store was deactivated — existence
        // (not active) + ownership (admin bypass) is the correct teardown gate.
        Store store = await _rules.StoreMustExistAsync(existing.StoreId, ct);
        bool isAdmin = _currentUser.IsInRole("Admin");
        _rules.MustBeOwner(store, _currentUser.UserId, isAdmin);

        Story? tracked = await _stories.GetAsync(s => s.Id == id, cancellationToken: ct);
        if (tracked is null) return;

        await _fileStorage.DeleteAsync(tracked.MediaUrl, ct);
        if (!string.IsNullOrEmpty(tracked.ThumbnailUrl))
            await _fileStorage.DeleteAsync(tracked.ThumbnailUrl, ct);
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
