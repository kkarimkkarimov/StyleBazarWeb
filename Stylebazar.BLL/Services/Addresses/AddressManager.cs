using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Addresses;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Rules;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Addresses;

public class AddressManager : IAddressService
{
    private readonly IAddressRepository _repo;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly AddressBusinessRules _rules;
    private readonly IValidator<CreateAddressDto> _createValidator;
    private readonly IValidator<UpdateAddressDto> _updateValidator;

    public AddressManager(
        IAddressRepository repo,
        IMapper mapper,
        ICurrentUserService currentUser,
        AddressBusinessRules rules,
        IValidator<CreateAddressDto> createValidator,
        IValidator<UpdateAddressDto> updateValidator)
    {
        _repo = repo;
        _mapper = mapper;
        _currentUser = currentUser;
        _rules = rules;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<AddressDto>> GetMineAsync(CancellationToken ct = default)
    {
        string userId = RequireUserId();
        Paginate<Address> page = await _repo.GetListAsync(
            predicate: a => a.UserId == userId,
            orderBy: q => q.OrderByDescending(a => a.IsDefault).ThenByDescending(a => a.CreatedDate),
            index: 0, size: int.MaxValue,
            enableTracking: false, cancellationToken: ct);
        return _mapper.Map<List<AddressDto>>(page.Items);
    }

    public async Task<AddressDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Address a = await _rules.AddressMustExistAsync(id, ct);
        _rules.MustBeOwner(a, _currentUser.UserId);
        return _mapper.Map<AddressDto>(a);
    }

    public async Task<AddressDto> CreateAsync(CreateAddressDto dto, CancellationToken ct = default)
    {
        string userId = RequireUserId();

        ValidationResult v = await _createValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        await _rules.QuotaNotExceededAsync(userId, ct);

        bool isFirstAddress = !await _repo.AnyAsync(a => a.UserId == userId, enableTracking: false, cancellationToken: ct);

        Address entity = new()
        {
            UserId = userId,
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone.Trim(),
            City = dto.City.Trim(),
            AddressLine = dto.AddressLine.Trim(),
            Label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(dto.PostalCode) ? null : dto.PostalCode.Trim(),
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
            // First address is always the default; subsequent ones respect the flag.
            IsDefault = dto.IsDefault || isFirstAddress
        };

        // Enforce single-default invariant
        if (entity.IsDefault)
            await ClearOtherDefaultsAsync(userId, excludeId: null, ct);

        Address created = await _repo.AddAsync(entity, ct);
        return _mapper.Map<AddressDto>(created);
    }

    public async Task<AddressDto> UpdateAsync(int id, UpdateAddressDto dto, CancellationToken ct = default)
    {
        ValidationResult v = await _updateValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        Address existing = await _rules.AddressMustExistAsync(id, ct);
        _rules.MustBeOwner(existing, _currentUser.UserId);

        // Re-fetch tracked so the update is observed.
        Address? tracked = await _repo.GetAsync(a => a.Id == id, cancellationToken: ct);
        if (tracked is null) throw new NotFoundException(Messages.Address.NotFound);

        tracked.FullName = dto.FullName.Trim();
        tracked.Phone = dto.Phone.Trim();
        tracked.City = dto.City.Trim();
        tracked.AddressLine = dto.AddressLine.Trim();
        tracked.Label = string.IsNullOrWhiteSpace(dto.Label) ? null : dto.Label.Trim();
        tracked.PostalCode = string.IsNullOrWhiteSpace(dto.PostalCode) ? null : dto.PostalCode.Trim();
        tracked.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
        tracked.IsDefault = dto.IsDefault;

        if (tracked.IsDefault)
            await ClearOtherDefaultsAsync(tracked.UserId, excludeId: tracked.Id, ct);

        await _repo.UpdateAsync(tracked, ct);
        return _mapper.Map<AddressDto>(tracked);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Address existing = await _rules.AddressMustExistAsync(id, ct);
        _rules.MustBeOwner(existing, _currentUser.UserId);

        Address? tracked = await _repo.GetAsync(a => a.Id == id, cancellationToken: ct);
        if (tracked is null) return;

        await _repo.DeleteAsync(tracked, permanent: false, cancellationToken: ct);

        // If we just removed the default, promote the most-recent remaining
        // address so checkout still has a sensible default.
        if (tracked.IsDefault)
        {
            Paginate<Address> remaining = await _repo.GetListAsync(
                predicate: a => a.UserId == tracked.UserId,
                orderBy: q => q.OrderByDescending(a => a.CreatedDate),
                index: 0, size: 1,
                enableTracking: true, cancellationToken: ct);
            Address? promote = remaining.Items.FirstOrDefault();
            if (promote is not null)
            {
                promote.IsDefault = true;
                await _repo.UpdateAsync(promote, ct);
            }
        }
    }

    public async Task SetDefaultAsync(int id, CancellationToken ct = default)
    {
        Address existing = await _rules.AddressMustExistAsync(id, ct);
        _rules.MustBeOwner(existing, _currentUser.UserId);

        Address? tracked = await _repo.GetAsync(a => a.Id == id, cancellationToken: ct);
        if (tracked is null) throw new NotFoundException(Messages.Address.NotFound);

        await ClearOtherDefaultsAsync(tracked.UserId, excludeId: tracked.Id, ct);
        tracked.IsDefault = true;
        await _repo.UpdateAsync(tracked, ct);
    }

    // -------- helpers --------

    private string RequireUserId()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Address.NotAuthenticated);
        return _currentUser.UserId;
    }

    private async Task ClearOtherDefaultsAsync(string userId, int? excludeId, CancellationToken ct)
    {
        Paginate<Address> defaults = await _repo.GetListAsync(
            predicate: a => a.UserId == userId && a.IsDefault && (excludeId == null || a.Id != excludeId),
            index: 0, size: int.MaxValue,
            enableTracking: true, cancellationToken: ct);
        foreach (Address d in defaults.Items)
        {
            d.IsDefault = false;
            await _repo.UpdateAsync(d, ct);
        }
    }
}
