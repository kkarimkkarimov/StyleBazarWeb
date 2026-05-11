using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Newsletter;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Newsletter;

public class NewsletterManager : INewsletterService
{
    private const int MaxPageSize = 200;

    private readonly INewsletterSubscriberRepository _repo;
    private readonly IMapper _mapper;
    private readonly IValidator<SubscribeDto> _validator;

    public NewsletterManager(
        INewsletterSubscriberRepository repo,
        IMapper mapper,
        IValidator<SubscribeDto> validator)
    {
        _repo = repo;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<NewsletterSubscriberDto> SubscribeAsync(SubscribeDto dto, CancellationToken ct = default)
    {
        ValidationResult validation = await _validator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        string normalised = dto.Email.Trim().ToLowerInvariant();

        // Reactivate or insert (idempotent)
        NewsletterSubscriber? existing = await _repo.GetAsync(
            n => n.Email == normalised,
            cancellationToken: ct);

        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.UnsubscribedAt = null;
                await _repo.UpdateAsync(existing, ct);
            }
            return _mapper.Map<NewsletterSubscriberDto>(existing);
        }

        NewsletterSubscriber entity = new()
        {
            Email = normalised,
            IsActive = true,
            UnsubscribeToken = Guid.NewGuid().ToString("N")
        };
        NewsletterSubscriber created = await _repo.AddAsync(entity, ct);
        return _mapper.Map<NewsletterSubscriberDto>(created);
    }

    public async Task UnsubscribeAsync(string token, CancellationToken ct = default)
    {
        NewsletterSubscriber? tracked = await _repo.GetAsync(
            n => n.UnsubscribeToken == token,
            cancellationToken: ct);
        if (tracked is null)
            throw new NotFoundException(Messages.Newsletter.NotFound);

        tracked.IsActive = false;
        tracked.UnsubscribedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(tracked, ct);
    }

    public async Task<Paginate<NewsletterSubscriberDto>> GetListAsync(int page, int pageSize, bool? isActive, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        Paginate<NewsletterSubscriber> p = await _repo.GetListAsync(
            predicate: n => !isActive.HasValue || n.IsActive == isActive.Value,
            orderBy: q => q.OrderByDescending(n => n.CreatedDate),
            index: page - 1,
            size: pageSize,
            enableTracking: false,
            cancellationToken: ct);

        return new Paginate<NewsletterSubscriberDto>
        {
            Index = p.Index,
            Size = p.Size,
            Count = p.Count,
            Pages = p.Pages,
            Items = _mapper.Map<List<NewsletterSubscriberDto>>(p.Items)
        };
    }
}
