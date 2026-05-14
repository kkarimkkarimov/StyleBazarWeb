using Core.Persistence.Paging;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Rules;

public class AddressBusinessRules
{
    private readonly IAddressRepository _addresses;
    private const int MaxAddressesPerUser = 10;

    public AddressBusinessRules(IAddressRepository addresses)
    {
        _addresses = addresses;
    }

    public async Task<Address> AddressMustExistAsync(int id, CancellationToken ct = default)
    {
        Address? a = await _addresses.GetAsync(x => x.Id == id, enableTracking: false, cancellationToken: ct);
        if (a is null) throw new NotFoundException(Messages.Address.NotFound);
        return a;
    }

    public void MustBeOwner(Address address, string? currentUserId)
    {
        if (string.IsNullOrEmpty(currentUserId))
            throw new UnauthorizedException(Messages.Address.NotAuthenticated);
        if (address.UserId != currentUserId)
            throw new BusinessException(Messages.Address.NotOwner);
    }

    public async Task QuotaNotExceededAsync(string userId, CancellationToken ct = default)
    {
        Paginate<Address> page = await _addresses.GetListAsync(
            predicate: a => a.UserId == userId,
            index: 0, size: 1, enableTracking: false,
            cancellationToken: ct);
        if (page.Count >= MaxAddressesPerUser)
            throw new BusinessException(string.Format(Messages.Address.QuotaExceeded, MaxAddressesPerUser));
    }
}
