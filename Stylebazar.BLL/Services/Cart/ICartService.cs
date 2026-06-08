using Stylebazar.BLL.DTOs.Cart;

namespace Stylebazar.BLL.Services.Cart;

// ICartService — səbət xidmətinin MÜQAVİLƏSİ. Bütün əməliyyatlar cari (giriş etmiş) istifadəçiyə aiddir.
public interface ICartService
{
    Task<CartSummaryDto> GetMyCartAsync(CancellationToken ct = default);   // səbəti gətir
    Task<int> GetMyCountAsync(CancellationToken ct = default);             // header rozetkası üçün ümumi say
    Task AddAsync(AddToCartDto dto, CancellationToken ct = default);       // əlavə et (eyni məhsul+ölçü+rəng birləşir)
    Task UpdateQuantityAsync(int cartItemId, int quantity, CancellationToken ct = default); // sayı dəyiş
    Task RemoveAsync(int cartItemId, CancellationToken ct = default);      // sil
    Task ClearAsync(string userId, CancellationToken ct = default);       // hamısını təmizlə (ödənişdən sonra)
}
