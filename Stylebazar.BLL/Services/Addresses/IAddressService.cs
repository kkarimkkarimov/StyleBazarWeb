using Stylebazar.BLL.DTOs.Addresses;

namespace Stylebazar.BLL.Services.Addresses;

/// <summary>
/// Per-user shipping / contact address book. All methods scope to the
/// authenticated user — there is no admin "see everyone's addresses" API.
/// </summary>
public interface IAddressService
{
    /// <summary>Returns the current user's addresses, default first then newest.</summary>
    Task<List<AddressDto>> GetMineAsync(CancellationToken ct = default);

    /// <summary>Returns the current user's default address, or null if none flagged.</summary>
    Task<AddressDto?> GetMyDefaultAsync(CancellationToken ct = default);

    /// <summary>Reads a single address by id. Throws NotFound if missing or not owned.</summary>
    Task<AddressDto> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new address. If it's the user's first one OR <c>dto.IsDefault</c> is
    /// true, the new row becomes the default (others are cleared automatically).
    /// </summary>
    Task<AddressDto> CreateAsync(CreateAddressDto dto, CancellationToken ct = default);

    /// <summary>Updates an existing address. Toggling <c>IsDefault</c> clears the previous default.</summary>
    Task<AddressDto> UpdateAsync(int id, UpdateAddressDto dto, CancellationToken ct = default);

    /// <summary>Soft-deletes an address. If it was the default, the newest remaining row is promoted.</summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>Marks the given address as default; clears the previous default in the same transaction.</summary>
    Task<AddressDto> SetDefaultAsync(int id, CancellationToken ct = default);
}
