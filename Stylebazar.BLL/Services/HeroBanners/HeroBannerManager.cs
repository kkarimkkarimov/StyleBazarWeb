using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.HeroBanners;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.HeroBanners;

public class HeroBannerManager : IHeroBannerService
{
    private readonly IHeroBannerRepository _repo;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateHeroBannerDto> _createValidator;
    private readonly IValidator<UpdateHeroBannerDto> _updateValidator;

    public HeroBannerManager(
        IHeroBannerRepository repo,
        IMapper mapper,
        IValidator<CreateHeroBannerDto> createValidator,
        IValidator<UpdateHeroBannerDto> updateValidator)
    {
        _repo = repo;
        _mapper = mapper;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<HeroBannerDto>> GetActiveAsync(CancellationToken ct = default)
    {
        DateTime now = DateTime.UtcNow;
        Paginate<HeroBanner> page = await _repo.GetListAsync(
            predicate: b =>
                b.IsActive &&
                (b.StartsAt == null || b.StartsAt <= now) &&
                (b.EndsAt == null || b.EndsAt >= now),
            orderBy: q => q.OrderBy(b => b.DisplayOrder).ThenByDescending(b => b.CreatedDate),
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);
        return _mapper.Map<List<HeroBannerDto>>(page.Items);
    }

    public async Task<List<HeroBannerDto>> GetAllAsync(CancellationToken ct = default)
    {
        Paginate<HeroBanner> page = await _repo.GetListAsync(
            orderBy: q => q.OrderBy(b => b.DisplayOrder).ThenByDescending(b => b.CreatedDate),
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);
        return _mapper.Map<List<HeroBannerDto>>(page.Items);
    }

    public async Task<HeroBannerDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        HeroBanner? b = await _repo.GetAsync(x => x.Id == id, enableTracking: false, cancellationToken: ct);
        if (b is null) throw new NotFoundException(Messages.HeroBanner.NotFound);
        return _mapper.Map<HeroBannerDto>(b);
    }

    public async Task<HeroBannerDto> CreateAsync(CreateHeroBannerDto dto, CancellationToken ct = default)
    {
        ValidationResult validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        HeroBanner entity = _mapper.Map<HeroBanner>(dto);
        entity.IsActive = true;
        HeroBanner created = await _repo.AddAsync(entity, ct);
        return _mapper.Map<HeroBannerDto>(created);
    }

    public async Task<HeroBannerDto> UpdateAsync(int id, UpdateHeroBannerDto dto, CancellationToken ct = default)
    {
        ValidationResult validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);

        HeroBanner? tracked = await _repo.GetAsync(x => x.Id == id, cancellationToken: ct);
        if (tracked is null) throw new NotFoundException(Messages.HeroBanner.NotFound);

        tracked.Title = dto.Title.Trim();
        tracked.Subtitle = dto.Subtitle?.Trim();
        tracked.Eyebrow = dto.Eyebrow?.Trim();
        tracked.ImageUrl = dto.ImageUrl?.Trim();
        tracked.CtaText = dto.CtaText?.Trim();
        tracked.CtaUrl = dto.CtaUrl?.Trim();
        tracked.DisplayOrder = dto.DisplayOrder;
        tracked.IsActive = dto.IsActive;
        tracked.StartsAt = dto.StartsAt;
        tracked.EndsAt = dto.EndsAt;

        await _repo.UpdateAsync(tracked, ct);
        return _mapper.Map<HeroBannerDto>(tracked);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        HeroBanner? tracked = await _repo.GetAsync(x => x.Id == id, cancellationToken: ct);
        if (tracked is null) throw new NotFoundException(Messages.HeroBanner.NotFound);
        await _repo.DeleteAsync(tracked, permanent: false, cancellationToken: ct);
    }
}
