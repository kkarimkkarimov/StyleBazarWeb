using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Addresses;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Addresses;

// AddressManager — xidmətin GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı). Yoxlama → repository/baza → nəticə axını burada idarə olunur.
public class AddressManager : IAddressService
{
    /// <summary>Hard cap on per-user address count. Stops bot-stuffed accounts.</summary>
    private const int MaxPerUser = 10;

    private readonly IAddressRepository _repo;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<CreateAddressDto> _createValidator;
    private readonly IValidator<UpdateAddressDto> _updateValidator;

    public AddressManager(
        IAddressRepository repo,
        IMapper mapper,
        ICurrentUserService currentUser,
        IValidator<CreateAddressDto> createValidator,
        IValidator<UpdateAddressDto> updateValidator)
    {
        _repo = repo;
        _mapper = mapper;
        _currentUser = currentUser;
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
            enableTracking: false,
            cancellationToken: ct);

        return _mapper.Map<List<AddressDto>>(page.Items);
    }

    public async Task<AddressDto?> GetMyDefaultAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId)) return null;
        string userId = _currentUser.UserId;

        Address? row = await _repo.GetAsync(
            a => a.UserId == userId && a.IsDefault,
            enableTracking: false,
            cancellationToken: ct);

        return row is null ? null : _mapper.Map<AddressDto>(row);
    }

    public async Task<AddressDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        string userId = RequireUserId();
        Address row = await RequireOwnedAsync(id, userId, ct, enableTracking: false);
        return _mapper.Map<AddressDto>(row);
    }

    public async Task<AddressDto> CreateAsync(CreateAddressDto dto, CancellationToken ct = default)
    {
        ValidationResult v = await _createValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        string userId = RequireUserId();

        // Hard cap — keeps the UI sane and stops scripted spam.
        // OrderBy required: EF translates size:1 into TOP/OFFSET, which SQL
        // Server forbids without ORDER BY (warns at runtime otherwise).
        Paginate<Address> existing = await _repo.GetListAsync(
            predicate: a => a.UserId == userId,
            orderBy: q => q.OrderBy(a => a.Id),
            index: 0, size: 1,
            enableTracking: false,
            cancellationToken: ct);
        if (existing.Count >= MaxPerUser)
            throw new BusinessException(Messages.Address.MaxLimitReached);

        Address entity = _mapper.Map<Address>(dto);
        entity.UserId = userId;

        // First address is always default; explicit IsDefault wins too.
        bool isFirst = existing.Count == 0;
        bool makeDefault = isFirst || dto.IsDefault;
        entity.IsDefault = makeDefault;

        if (makeDefault)
            await ClearDefaultAsync(userId, exceptId: null, ct);

        // Trim whitespace once at the entry — keeps storage clean.
        entity.Title = entity.Title.Trim();
        entity.FullName = entity.FullName.Trim();
        entity.Phone = entity.Phone.Trim();
        entity.City = entity.City.Trim();
        entity.AddressLine = entity.AddressLine.Trim();
        entity.District = NullIfBlank(entity.District);
        entity.PostalCode = NullIfBlank(entity.PostalCode);
        entity.Notes = NullIfBlank(entity.Notes);

        Address created = await _repo.AddAsync(entity, ct);
        return _mapper.Map<AddressDto>(created);
    }

    public async Task<AddressDto> UpdateAsync(int id, UpdateAddressDto dto, CancellationToken ct = default)
    {
        ValidationResult v = await _updateValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        string userId = RequireUserId();
        Address row = await RequireOwnedAsync(id, userId, ct, enableTracking: true);

        row.Title = dto.Title.Trim();
        row.FullName = dto.FullName.Trim();
        row.Phone = dto.Phone.Trim();
        row.City = dto.City.Trim();
        row.District = NullIfBlank(dto.District);
        row.AddressLine = dto.AddressLine.Trim();
        row.PostalCode = NullIfBlank(dto.PostalCode);
        row.Notes = NullIfBlank(dto.Notes);

        // If user toggled default ON, clear the previous default first.
        if (dto.IsDefault && !row.IsDefault)
        {
            await ClearDefaultAsync(userId, exceptId: id, ct);
            row.IsDefault = true;
        }
        else if (!dto.IsDefault && row.IsDefault)
        {
            // Don't allow unsetting the last default — silently keep it.
            // (UI hides the toggle for the lone default address anyway.)
            row.IsDefault = true;
        }

        await _repo.UpdateAsync(row, ct);
        return _mapper.Map<AddressDto>(row);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        string userId = RequireUserId();
        Address row = await RequireOwnedAsync(id, userId, ct, enableTracking: true);

        bool wasDefault = row.IsDefault;
        await _repo.DeleteAsync(row, permanent: false, cancellationToken: ct);

        // Promote the newest surviving row if we just removed the default.
        if (wasDefault)
        {
            Paginate<Address> remaining = await _repo.GetListAsync(
                predicate: a => a.UserId == userId,
                orderBy: q => q.OrderByDescending(a => a.CreatedDate),
                index: 0, size: 1,
                enableTracking: true,
                cancellationToken: ct);
            Address? next = remaining.Items.FirstOrDefault();
            if (next is not null)
            {
                next.IsDefault = true;
                await _repo.UpdateAsync(next, ct);
            }
        }
    }

    public async Task<AddressDto> SetDefaultAsync(int id, CancellationToken ct = default)
    {
        string userId = RequireUserId();
        Address row = await RequireOwnedAsync(id, userId, ct, enableTracking: true);

        if (row.IsDefault) return _mapper.Map<AddressDto>(row);

        await ClearDefaultAsync(userId, exceptId: id, ct);
        row.IsDefault = true;
        await _repo.UpdateAsync(row, ct);

        return _mapper.Map<AddressDto>(row);
    }

    // -------- helpers --------

    private string RequireUserId()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Address.NotAuthenticated);
        return _currentUser.UserId;
    }

    private async Task<Address> RequireOwnedAsync(int id, string userId, CancellationToken ct, bool enableTracking)
    {
        Address? row = await _repo.GetAsync(
            a => a.Id == id,
            enableTracking: enableTracking,
            cancellationToken: ct);

        if (row is null)
            throw new NotFoundException(Messages.Address.NotFound);
        if (row.UserId != userId)
            throw new BusinessException(Messages.Address.NotOwner);

        return row;
    }

    private async Task ClearDefaultAsync(string userId, int? exceptId, CancellationToken ct)
    {
        Paginate<Address> defaults = await _repo.GetListAsync(
            predicate: a => a.UserId == userId && a.IsDefault && (exceptId == null || a.Id != exceptId),
            orderBy: q => q.OrderBy(a => a.Id),
            index: 0, size: int.MaxValue,
            enableTracking: true,
            cancellationToken: ct);

        foreach (Address row in defaults.Items)
        {
            row.IsDefault = false;
            await _repo.UpdateAsync(row, ct);
        }
    }

    private static string? NullIfBlank(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
