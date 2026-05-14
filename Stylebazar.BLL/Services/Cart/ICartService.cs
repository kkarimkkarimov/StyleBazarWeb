using Stylebazar.BLL.DTOs.Cart;

namespace Stylebazar.BLL.Services.Cart;

public interface ICartService
{
    /// <summary>The current user's cart (auto-created on first access). Throws if anon.</summary>
    Task<CartDto> GetMineAsync(CancellationToken ct = default);

    /// <summary>Add an item (merges into existing line for same product+size).</summary>
    Task<CartDto> AddAsync(AddToCartDto dto, CancellationToken ct = default);

    /// <summary>Replace quantity on a line.</summary>
    Task<CartDto> UpdateQuantityAsync(int itemId, UpdateCartItemDto dto, CancellationToken ct = default);

    /// <summary>Remove a single line.</summary>
    Task<CartDto> RemoveAsync(int itemId, CancellationToken ct = default);

    /// <summary>Empty the current user's cart.</summary>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>Total item count for header badge. 0 for anon.</summary>
    Task<int> GetMyCountAsync(CancellationToken ct = default);
}
