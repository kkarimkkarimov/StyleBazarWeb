using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.DTOs.Cart;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;

namespace Stylebazar.BLL.Services.Cart;

// CartManager — ICartService-in GERÇƏKLƏŞMƏSİ. Səbət DB-əsaslıdır, cari istifadəçiyə bağlıdır.
public class CartManager : ICartService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CartManager(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private string RequireUserId()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException("Səbəti idarə etmək üçün hesabınıza daxil olun.");
        return _currentUser.UserId;
    }

    // Səbəti gətirir — cari qiymət (endirim varsa o), şəkil, mağaza, stok ilə birgə (tək sorğu, proyeksiya).
    public async Task<CartSummaryDto> GetMyCartAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId)) return new CartSummaryDto();
        string userId = _currentUser.UserId;

        List<CartItemDto> items = await _db.CartItems.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new CartItemDto
            {
                Id = c.Id,
                ProductId = c.ProductId,
                ProductName = c.Product.Name,
                ProductSlug = c.Product.Slug,
                ImageUrl = c.Product.Images.Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
                           ?? c.Product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault(),
                UnitPrice = c.Product.DiscountPrice ?? c.Product.Price,
                Quantity = c.Quantity,
                Size = c.Size,
                Color = c.Color,
                StoreId = c.Product.StoreId,
                StoreName = c.Product.Store.Name,
                StockQuantity = c.Product.StockQuantity
            })
            .ToListAsync(ct);

        return new CartSummaryDto { Items = items };
    }

    // Header rozetkası üçün ümumi say (cəm).
    public async Task<int> GetMyCountAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId)) return 0;
        string userId = _currentUser.UserId;
        return await _db.CartItems.Where(c => c.UserId == userId).SumAsync(c => (int?)c.Quantity, ct) ?? 0;
    }

    // Səbətə əlavə et — eyni məhsul+ölçü+rəng varsa sayı artırır (stokla məhdud).
    public async Task AddAsync(AddToCartDto dto, CancellationToken ct = default)
    {
        string userId = RequireUserId();

        Product? product = await _db.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId, ct);
        if (product is null || !product.IsActive)
            throw new NotFoundException("Məhsul tapılmadı və ya aktiv deyil.");
        if (product.StockQuantity <= 0)
            throw new BusinessException("Bu məhsul hazırda stokda yoxdur.");

        int qty = dto.Quantity < 1 ? 1 : dto.Quantity;
        string? size = string.IsNullOrWhiteSpace(dto.Size) ? null : dto.Size.Trim();
        string? color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color.Trim();

        CartItem? existing = await _db.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == dto.ProductId
                                      && c.Size == size && c.Color == color, ct);

        if (existing is not null)
            existing.Quantity = Math.Min(existing.Quantity + qty, product.StockQuantity);
        else
            _db.CartItems.Add(new CartItem
            {
                UserId = userId,
                ProductId = dto.ProductId,
                Quantity = Math.Min(qty, product.StockQuantity),
                Size = size,
                Color = color
            });

        await _db.SaveChangesAsync(ct);
    }

    // Sayı dəyiş (0 və ya az → sil). Stokla məhdudlaşır.
    public async Task UpdateQuantityAsync(int cartItemId, int quantity, CancellationToken ct = default)
    {
        string userId = RequireUserId();
        CartItem? item = await _db.CartItems.Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId, ct);
        if (item is null) throw new NotFoundException("Səbət elementi tapılmadı.");

        if (quantity <= 0)
        {
            _db.CartItems.Remove(item);
        }
        else
        {
            int max = item.Product.StockQuantity > 0 ? item.Product.StockQuantity : 1;
            item.Quantity = Math.Min(quantity, max);
        }
        await _db.SaveChangesAsync(ct);
    }

    // Bir elementi sil.
    public async Task RemoveAsync(int cartItemId, CancellationToken ct = default)
    {
        string userId = RequireUserId();
        CartItem? item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId, ct);
        if (item is null) return;
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }

    // Bütün səbəti təmizlə (ödəniş tamamlananda çağırılır).
    public async Task ClearAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return;
        await _db.CartItems.Where(c => c.UserId == userId).ExecuteDeleteAsync(ct);
    }
}
