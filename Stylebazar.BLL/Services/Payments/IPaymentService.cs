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
}

// Tamamlanmış ödənişin nəticəsi: kimin (UserId), hansı planı (Plan) və unikal ödəniş istinadı (idempotentlik üçün).
public record PaidCheckout(string UserId, SubscriptionPlan Plan, string PaymentReference);
