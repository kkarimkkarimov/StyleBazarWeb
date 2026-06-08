using Stylebazar.BLL.DTOs.Orders;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Services.Orders;

// IOrderService — sifariş xidmətinin MÜQAVİLƏSİ.
// Alıcı tərəfi cari istifadəçiyə bağlıdır; satıcı/admin tərəfi rol əsaslı.
public interface IOrderService
{
    // ---- Alıcı ----

    // Cari istifadəçinin səbətindən "Pending" (ödəniş gözləyən) sifariş yaradır, id-sini qaytarır.
    // Stok azaldılmır (ödəniş tamamlananda azalır). Çatdırılma ünvanı snapshot kimi saxlanır.
    Task<int> CreateFromCartAsync(PlaceOrderDto dto, CancellationToken ct = default);

    // Cari istifadəçinin sifariş tarixçəsi (ən yenisi öndə).
    Task<List<OrderListItemDto>> GetMyOrdersAsync(CancellationToken ct = default);

    // Cari istifadəçinin bir sifarişinin tam detalı (başqasının sifarişi → null).
    Task<OrderDetailDto?> GetMyOrderDetailAsync(int orderId, CancellationToken ct = default);

    // ---- Ödəniş (Stripe success/webhook çağırır — istifadəçi-scope YOX, orderId Stripe-dan təsdiqlənib) ----

    // Sifarişi "ödənildi" işarələ: IsPaid, PaidAt, Status=Processing, stok azalt, səbəti təmizlə.
    // İdempotent — artıq ödənilibsə heç nə etmir (eyni webhook iki dəfə gəlsə təhlükəsiz).
    Task MarkPaidAsync(int orderId, string paymentReference, CancellationToken ct = default);

    // ---- Satıcı ----

    // Cari satıcının mağaza(ları)na aid sifariş sətirlərini ehtiva edən sifarişlər.
    Task<List<OrderListItemDto>> GetStoreOrdersAsync(CancellationToken ct = default);

    // Bir sifarişin satıcı görünüşü: yalnız satıcının öz məhsulları + çatdırılma (admin hamısını görür).
    // Satıcının payı olmayan sifariş → null.
    Task<OrderDetailDto?> GetStoreOrderDetailAsync(int orderId, CancellationToken ct = default);

    // Satıcı/admin sifariş statusunu irəli aparır (Processing→Shipped→Delivered, və ya Cancelled).
    Task UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken ct = default);
}
