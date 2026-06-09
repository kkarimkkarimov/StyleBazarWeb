using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.DTOs.Orders;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Services.Orders;

// OrderManager — IOrderService-in GERÇƏKLƏŞMƏSİ. Səbət → sifariş → ödəniş → status axını.
public class OrderManager : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public OrderManager(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private string RequireUserId()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException("Sifariş üçün hesabınıza daxil olun.");
        return _currentUser.UserId;
    }

    // Səbətdən "Pending" sifariş yaradır. Qiymət/şəkil/ad snapshot kimi köçürülür.
    public async Task<int> CreateFromCartAsync(PlaceOrderDto dto, CancellationToken ct = default)
    {
        string userId = RequireUserId();

        // Səbəti məhsul + şəkil + mağaza ilə birgə yüklə (snapshot üçün lazımdır).
        List<CartItem> cartItems = await _db.CartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.Product).ThenInclude(p => p.Images)
            .Include(c => c.Product).ThenInclude(p => p.Store)
            .ToListAsync(ct);

        if (cartItems.Count == 0)
            throw new BusinessException("Səbətiniz boşdur.");

        // --- Çatdırılma ünvanı snapshot-u: ya seçilmiş ünvandan, ya yeni sahələrdən ---
        string shipFullName, shipPhone, shipCity, shipAddressLine;
        string? shipDistrict, shipPostalCode, shipNotes;

        if (dto.AddressId is int addrId && addrId > 0)
        {
            Address addr = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == addrId && a.UserId == userId, ct)
                ?? throw new NotFoundException("Seçilmiş ünvan tapılmadı.");
            shipFullName = addr.FullName;
            shipPhone = addr.Phone;
            shipCity = addr.City;
            shipDistrict = addr.District;
            shipAddressLine = addr.AddressLine;
            shipPostalCode = addr.PostalCode;
            shipNotes = addr.Notes;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.ShipFullName) || string.IsNullOrWhiteSpace(dto.ShipPhone)
                || string.IsNullOrWhiteSpace(dto.ShipCity) || string.IsNullOrWhiteSpace(dto.ShipAddressLine))
                throw new BusinessException("Çatdırılma ünvanı tam doldurulmalıdır (ad, telefon, şəhər, ünvan).");

            shipFullName = dto.ShipFullName!.Trim();
            shipPhone = dto.ShipPhone!.Trim();
            shipCity = dto.ShipCity!.Trim();
            shipDistrict = string.IsNullOrWhiteSpace(dto.ShipDistrict) ? null : dto.ShipDistrict!.Trim();
            shipAddressLine = dto.ShipAddressLine!.Trim();
            shipPostalCode = string.IsNullOrWhiteSpace(dto.ShipPostalCode) ? null : dto.ShipPostalCode!.Trim();
            shipNotes = string.IsNullOrWhiteSpace(dto.ShipNotes) ? null : dto.ShipNotes!.Trim();

            // İstifadəçi istəyibsə yeni ünvanı kitabçaya da əlavə et (ilk ünvandırsa default olur).
            if (dto.SaveAddress)
            {
                bool hasAny = await _db.Addresses.AnyAsync(a => a.UserId == userId, ct);
                _db.Addresses.Add(new Address
                {
                    UserId = userId,
                    Title = "Çatdırılma",
                    FullName = shipFullName,
                    Phone = shipPhone,
                    City = shipCity,
                    District = shipDistrict,
                    AddressLine = shipAddressLine,
                    PostalCode = shipPostalCode,
                    Notes = shipNotes,
                    IsDefault = !hasAny
                });
            }
        }

        // --- Sifariş + sətirlər ---
        var order = new Order
        {
            BuyerId = userId,
            Status = OrderStatus.Pending,
            IsPaid = false,
            ShipFullName = shipFullName,
            ShipPhone = shipPhone,
            ShipCity = shipCity,
            ShipDistrict = shipDistrict,
            ShipAddressLine = shipAddressLine,
            ShipPostalCode = shipPostalCode,
            ShipNotes = shipNotes
        };

        decimal total = 0m;
        foreach (CartItem c in cartItems)
        {
            Product? p = c.Product;
            if (p is null || !p.IsActive)
                throw new BusinessException($"'{p?.Name ?? "Məhsul"}' artıq mövcud deyil. Səbətdən çıxarın.");
            if (p.StockQuantity <= 0)
                throw new BusinessException($"'{p.Name}' stokda yoxdur. Səbətdən çıxarın.");

            int qty = Math.Min(c.Quantity, p.StockQuantity);
            decimal unit = p.DiscountPrice ?? p.Price;
            string? img = p.Images.Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
                          ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault();

            order.Items.Add(new OrderItem
            {
                ProductId = p.Id,
                StoreId = p.StoreId,
                ProductName = p.Name,
                ProductSlug = p.Slug,
                ImageUrl = img,
                UnitPrice = unit,
                Quantity = qty,
                Size = c.Size,
                Color = c.Color
            });
            total += unit * qty;
        }

        order.TotalAmount = total;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order.Id;
    }

    // Alıcının sifariş tarixçəsi.
    public async Task<List<OrderListItemDto>> GetMyOrdersAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId)) return new();
        string userId = _currentUser.UserId;

        return await _db.Orders.AsNoTracking()
            .Where(o => o.BuyerId == userId)
            .OrderByDescending(o => o.CreatedDate)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                CreatedDate = o.CreatedDate,
                Status = o.Status,
                IsPaid = o.IsPaid,
                TotalAmount = o.TotalAmount,
                TotalQuantity = o.Items.Sum(i => i.Quantity),
                ItemCount = o.Items.Count,
                FirstImageUrl = o.Items.Select(i => i.ImageUrl).FirstOrDefault()
            })
            .ToListAsync(ct);
    }

    // Alıcının bir sifarişinin detalı (yalnız öz sifarişi).
    public async Task<OrderDetailDto?> GetMyOrderDetailAsync(int orderId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId)) return null;
        string userId = _currentUser.UserId;

        return await _db.Orders.AsNoTracking()
            .Where(o => o.Id == orderId && o.BuyerId == userId)
            .Select(DetailProjection)
            .FirstOrDefaultAsync(ct);
    }

    // Sifarişi "ödənildi" işarələ (idempotent). Stripe success/webhook çağırır.
    public async Task MarkPaidAsync(int orderId, string paymentReference, CancellationToken ct = default)
    {
        Order? order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) throw new NotFoundException("Sifariş tapılmadı.");
        if (order.IsPaid) return;   // idempotent — təkrar webhook təhlükəsiz

        order.IsPaid = true;
        order.PaidAt = DateTime.UtcNow;
        order.Status = OrderStatus.Processing;
        order.PaymentReference = paymentReference;

        // Stok azalt (hər məhsul üçün, 0-dan aşağı düşməsin).
        foreach (OrderItem item in order.Items)
        {
            Product? p = await _db.Products.FirstOrDefaultAsync(x => x.Id == item.ProductId, ct);
            if (p is not null)
                p.StockQuantity = Math.Max(0, p.StockQuantity - item.Quantity);
        }

        await _db.SaveChangesAsync(ct);

        // Ödəniş tamamlandı → alıcının səbətini təmizlə.
        await _db.CartItems.Where(c => c.UserId == order.BuyerId).ExecuteDeleteAsync(ct);
    }

    // Satıcının mağaza(ları)na aid məhsulları olan sifarişlər.
    public async Task<List<OrderListItemDto>> GetStoreOrdersAsync(CancellationToken ct = default)
    {
        string userId = RequireUserId();

        // Satıcının mağaza id-ləri.
        List<int> storeIds = await _db.Stores.Where(s => s.OwnerId == userId).Select(s => s.Id).ToListAsync(ct);
        if (storeIds.Count == 0) return new();

        return await _db.Orders.AsNoTracking()
            .Where(o => o.IsPaid && o.Items.Any(i => storeIds.Contains(i.StoreId)))
            .OrderByDescending(o => o.CreatedDate)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                CreatedDate = o.CreatedDate,
                Status = o.Status,
                IsPaid = o.IsPaid,
                // Satıcı yalnız öz məhsullarının cəmini görür (digər satıcıların məbləği gizli).
                TotalAmount = o.Items.Where(i => storeIds.Contains(i.StoreId)).Sum(i => i.UnitPrice * i.Quantity),
                TotalQuantity = o.Items.Where(i => storeIds.Contains(i.StoreId)).Sum(i => i.Quantity),
                ItemCount = o.Items.Count(i => storeIds.Contains(i.StoreId)),
                FirstImageUrl = o.Items.Where(i => storeIds.Contains(i.StoreId)).Select(i => i.ImageUrl).FirstOrDefault(),
                BuyerName = o.Buyer.FullName
            })
            .ToListAsync(ct);
    }

    // Bütün sifarişlər (admin icmalı) — opsional status filtri ilə.
    public async Task<List<OrderListItemDto>> GetAllForAdminAsync(OrderStatus? status, CancellationToken ct = default)
    {
        IQueryable<Order> q = _db.Orders.AsNoTracking();
        if (status.HasValue) q = q.Where(o => o.Status == status.Value);

        return await q
            .OrderByDescending(o => o.CreatedDate)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                CreatedDate = o.CreatedDate,
                Status = o.Status,
                IsPaid = o.IsPaid,
                TotalAmount = o.TotalAmount,
                TotalQuantity = o.Items.Sum(i => i.Quantity),
                ItemCount = o.Items.Count,
                FirstImageUrl = o.Items.Select(i => i.ImageUrl).FirstOrDefault(),
                BuyerName = o.Buyer.FullName
            })
            .ToListAsync(ct);
    }

    // Bir sifarişin satıcı görünüşü: yalnız öz məhsulları (admin hamısını görür).
    public async Task<OrderDetailDto?> GetStoreOrderDetailAsync(int orderId, CancellationToken ct = default)
    {
        string userId = RequireUserId();
        bool isAdmin = _currentUser.IsInRole("Admin");

        List<int> storeIds = isAdmin
            ? new List<int>()
            : await _db.Stores.Where(s => s.OwnerId == userId).Select(s => s.Id).ToListAsync(ct);

        OrderDetailDto? order = await _db.Orders.AsNoTracking()
            .Where(o => o.Id == orderId && (isAdmin || o.Items.Any(i => storeIds.Contains(i.StoreId))))
            .Select(DetailProjection)
            .FirstOrDefaultAsync(ct);

        if (order is null) return null;

        // Satıcı yalnız öz məhsullarını və onların cəmini görür (digər satıcıların məlumatı gizli).
        if (!isAdmin)
        {
            order.Items = order.Items.Where(i => storeIds.Contains(i.StoreId)).ToList();
            order.TotalAmount = order.Items.Sum(i => i.LineTotal);
        }

        return order;
    }

    // Sifariş statusunu irəli aparır (satıcı/admin). Tarixləri də yeniləyir.
    public async Task UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken ct = default)
    {
        string userId = RequireUserId();

        Order? order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) throw new NotFoundException("Sifariş tapılmadı.");

        // İcazə: ya admin, ya da sifarişdə öz məhsulu olan satıcı.
        bool isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin)
        {
            List<int> storeIds = await _db.Stores.Where(s => s.OwnerId == userId).Select(s => s.Id).ToListAsync(ct);
            bool ownsItem = order.Items.Any(i => storeIds.Contains(i.StoreId));
            if (!ownsItem)
                throw new UnauthorizedException("Bu sifarişi idarə etmək icazəniz yoxdur.");
        }

        // Ödənilməmiş sifarişi yalnız ləğv etmək olar.
        if (!order.IsPaid && status != OrderStatus.Cancelled)
            throw new BusinessException("Ödənilməmiş sifarişin statusu dəyişdirilə bilməz.");

        order.Status = status;
        DateTime now = DateTime.UtcNow;
        switch (status)
        {
            case OrderStatus.Shipped: order.ShippedAt = now; break;
            case OrderStatus.Delivered: order.DeliveredAt = now; break;
            case OrderStatus.Cancelled: order.CancelledAt = now; break;
        }

        await _db.SaveChangesAsync(ct);
    }

    // Ortaq proyeksiya: Order → OrderDetailDto. Expression-dur ki, EF SQL-ə tərcümə edə bilsin.
    private static readonly System.Linq.Expressions.Expression<Func<Order, OrderDetailDto>> DetailProjection = o => new OrderDetailDto
    {
        Id = o.Id,
        CreatedDate = o.CreatedDate,
        Status = o.Status,
        IsPaid = o.IsPaid,
        PaymentReference = o.PaymentReference,
        TotalAmount = o.TotalAmount,
        ShipFullName = o.ShipFullName,
        ShipPhone = o.ShipPhone,
        ShipCity = o.ShipCity,
        ShipDistrict = o.ShipDistrict,
        ShipAddressLine = o.ShipAddressLine,
        ShipPostalCode = o.ShipPostalCode,
        ShipNotes = o.ShipNotes,
        PaidAt = o.PaidAt,
        ShippedAt = o.ShippedAt,
        DeliveredAt = o.DeliveredAt,
        CancelledAt = o.CancelledAt,
        Items = o.Items.Select(i => new OrderItemDto
        {
            ProductId = i.ProductId,
            StoreId = i.StoreId,
            ProductName = i.ProductName,
            ProductSlug = i.ProductSlug,
            ImageUrl = i.ImageUrl,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity,
            Size = i.Size,
            Color = i.Color,
            StoreName = i.Store.Name
        }).ToList()
    };
}
