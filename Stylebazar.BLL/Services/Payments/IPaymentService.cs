using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Services.Payments;

// IPaymentService — ödəniş (Stripe) xidmətinin MÜQAVİLƏSİ.
// Checkout sessiyası yaratmaq + ödənişin nəticəsini həm success səhifəsindən, həm webhook-dan oxumaq.
public interface IPaymentService
{
    // Seçilmiş plan üçün Stripe Checkout sessiyası yaradır və istifadəçinin yönləndiriləcəyi URL-i qaytarır.
    // userEmail + userName Stripe dashboard-da ödəyən satıcının kim olduğunu göstərmək üçün ötürülür.
    Task<string> CreateCheckoutUrlAsync(
        string userId, string? userEmail, string? userName,
        SubscriptionPlan plan, string successUrl, string cancelUrl, CancellationToken ct = default);

    // Checkout sessiyasını Stripe-dan oxuyur (success səhifəsi üçün): ödəniş tamamlanıbsa nəticə, yoxsa null.
    Task<PaidCheckout?> GetCompletedCheckoutAsync(string sessionId, CancellationToken ct = default);

    // Stripe webhook gövdəsini imza ilə YOXLAYIR və tamamlanmış ödənişi qaytarır (yoxsa null).
    // İmza səhvdirsə Stripe exception atır — çağıran 400 qaytarmalıdır (saxta webhook-a qarşı qoruma).
    PaidCheckout? ParseCompletedCheckoutWebhook(string requestJson, string stripeSignatureHeader);

    // ---- Saytdaxili SİFARİŞ ödənişi (səbət → checkout) ----

    // Sifariş üçün Stripe Checkout sessiyası yaradır (hər məhsul ayrıca line item kimi).
    Task<string> CreateOrderCheckoutUrlAsync(
        OrderCheckoutRequest request, string successUrl, string cancelUrl, CancellationToken ct = default);

    // Success səhifəsi üçün: sifariş sessiyasını Stripe-dan oxu, ödəniş "paid"-dirsə nəticəni qaytar.
    Task<PaidOrderCheckout?> GetCompletedOrderCheckoutAsync(string sessionId, CancellationToken ct = default);

    // Webhook: imzanı yoxla, yalnız sifariş (type=order) ödənişini qaytar (yoxsa null).
    PaidOrderCheckout? ParseCompletedOrderWebhook(string requestJson, string stripeSignatureHeader);
}

// Tamamlanmış ödənişin nəticəsi: kimin (UserId), hansı planı (Plan) və unikal ödəniş istinadı (idempotentlik üçün).
public record PaidCheckout(string UserId, SubscriptionPlan Plan, string PaymentReference);

// Sifariş üçün Stripe Checkout girişi: sifariş id-si, alıcı email/ad (dashboard üçün) və sətirlər.
public record OrderCheckoutRequest(
    int OrderId, string? BuyerEmail, string? BuyerName, IReadOnlyList<OrderCheckoutLine> Lines);

// Stripe-da bir line item: ad (ölçü/rəng daxil), vahid qiymət, say.
public record OrderCheckoutLine(string Name, decimal UnitPrice, int Quantity);

// Tamamlanmış sifariş ödənişinin nəticəsi: hansı sifariş (OrderId) və ödəniş istinadı (idempotentlik üçün).
public record PaidOrderCheckout(int OrderId, string PaymentReference);
