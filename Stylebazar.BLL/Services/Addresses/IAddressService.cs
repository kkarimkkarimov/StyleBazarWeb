using Stylebazar.BLL.DTOs.Addresses;

namespace Stylebazar.BLL.Services.Addresses;

public interface IAddressService
{
    Task<List<AddressDto>> GetMineAsync(CancellationToken ct = default);
    Task<AddressDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<AddressDto> CreateAsync(CreateAddressDto dto, CancellationToken ct = default);
    Task<AddressDto> UpdateAsync(int id, UpdateAddressDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SetDefaultAsync(int id, CancellationToken ct = default);
}
