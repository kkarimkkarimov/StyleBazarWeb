using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Contact;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Contact;

public class ContactMessageManager : IContactMessageService
{
    private const int MaxPageSize = 100;

    private readonly IContactMessageRepository _repo;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateContactMessageDto> _createValidator;

    public ContactMessageManager(
        IContactMessageRepository repo,
        IMapper mapper,
        IValidator<CreateContactMessageDto> createValidator)
    {
        _repo = repo;
        _mapper = mapper;
        _createValidator = createValidator;
    }

    public async Task<ContactMessageDto> SubmitAsync(CreateContactMessageDto dto, string? ipAddress, CancellationToken ct = default)
    {
        ValidationResult validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        ContactMessage entity = new()
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            Subject = dto.Subject?.Trim(),
            Message = dto.Message.Trim(),
            Status = ContactStatus.New,
            UserIpAddress = ipAddress
        };
        ContactMessage created = await _repo.AddAsync(entity, ct);
        return _mapper.Map<ContactMessageDto>(created);
    }

    public async Task<Paginate<ContactMessageDto>> GetListAsync(int page, int pageSize, ContactStatus? status, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        Paginate<ContactMessage> p = await _repo.GetListAsync(
            predicate: m => !status.HasValue || m.Status == status.Value,
            orderBy: q => q.OrderByDescending(m => m.CreatedDate),
            index: page - 1,
            size: pageSize,
            enableTracking: false,
            cancellationToken: ct);

        return new Paginate<ContactMessageDto>
        {
            Index = p.Index,
            Size = p.Size,
            Count = p.Count,
            Pages = p.Pages,
            Items = _mapper.Map<List<ContactMessageDto>>(p.Items)
        };
    }

    public async Task<ContactMessageDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        ContactMessage? m = await _repo.GetAsync(x => x.Id == id, enableTracking: false, cancellationToken: ct);
        if (m is null) throw new NotFoundException(Messages.Contact.NotFound);
        return _mapper.Map<ContactMessageDto>(m);
    }

    public async Task<ContactMessageDto> UpdateStatusAsync(int id, UpdateContactMessageStatusDto dto, CancellationToken ct = default)
    {
        ContactMessage? tracked = await _repo.GetAsync(x => x.Id == id, cancellationToken: ct);
        if (tracked is null) throw new NotFoundException(Messages.Contact.NotFound);

        tracked.Status = dto.Status;
        tracked.AdminNote = dto.AdminNote?.Trim();
        await _repo.UpdateAsync(tracked, ct);
        return _mapper.Map<ContactMessageDto>(tracked);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        ContactMessage? tracked = await _repo.GetAsync(x => x.Id == id, cancellationToken: ct);
        if (tracked is null) throw new NotFoundException(Messages.Contact.NotFound);
        await _repo.DeleteAsync(tracked, permanent: false, cancellationToken: ct);
    }
}
