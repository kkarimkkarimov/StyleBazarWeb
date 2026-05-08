using Stylebazar.BLL.DTOs.Wishlist;

namespace Stylebazar.BLL.Services.Wishlist;

public interface IWishlistService
{
    Task<List<WishlistItemDto>> GetMineAsync(CancellationToken ct = default);
    Task<WishlistItemDto> AddAsync(int productId, CancellationToken ct = default);
    Task RemoveAsync(int productId, CancellationToken ct = default);
}
