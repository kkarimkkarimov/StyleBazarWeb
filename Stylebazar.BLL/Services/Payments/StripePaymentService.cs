using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Stylebazar.BLL.DTOs.Subscriptions;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Subscriptions;
using Stylebazar.BLL.Settings;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Services.Payments;

// StripePaymentService — IPaymentService-in Stripe ilə GERÇƏKLƏŞMƏSİ.
// Stripe Checkout (hosted) istifadə edir: kart məlumatı bizim serverə YOX, birbaşa Stripe-a gedir (PCI-təhlükəsiz).
public class StripePaymentService : IPaymentService
{
    private readonly StripeSettings _settings;

    public StripePaymentService(IOptions<StripeSettings> settings)
    {
        _settings = settings.Value;
        // Stripe SDK qlobal gizli API açarı (bütün sorğular bununla imzalanır).
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    // Birdəfəlik (1 aylıq) ödəniş üçün Checkout sessiyası yaradır.
    public async Task<string> CreateCheckoutUrlAsync(
        string userId, string? userEmail, string? userName,
        SubscriptionPlan plan, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        if (plan == SubscriptionPlan.Free)
            throw new BusinessException("Pulsuz plan üçün ödəniş tələb olunmur.");

        // Aydın xəta: açar yüklənməyibsə (user-secrets/env yoxdur, və ya app restart olunmayıb).
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            throw new BusinessException("Stripe SecretKey konfiqurasiya olunmayıb — user-secrets/env yoxla və app-ı TAM yenidən başlat.");

        PlanInfoDto info = PlanCatalog.For(plan);
        // Stripe məbləği ən kiçik vahidlə (sent) istəyir: 15.00 → 1500.
        long amountMinor = (long)(info.MonthlyPriceAzn * 100m);

        var options = new SessionCreateOptions
        {
            Mode = "payment",                                   // birdəfəlik ödəniş (recurring deyil)
            PaymentMethodTypes = new List<string> { "card" },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = userId,
            // Stripe dashboard-da ödəyənin email-i görünsün + qəbz ora getsin.
            CustomerEmail = string.IsNullOrWhiteSpace(userEmail) ? null : userEmail,
            // Webhook + success səhifəsi planı tətbiq edəndə kimi/nəyi biləcək; həm də
            // dashboard-da satıcının id/email/adı Metadata bölməsində görünəcək.
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "plan",
                ["userId"] = userId,
                ["plan"] = plan.ToString(),
                ["userEmail"] = userEmail ?? string.Empty,
                ["userName"] = userName ?? string.Empty
            },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = _settings.Currency,
                        UnitAmount = amountMinor,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"StyleBazar {info.Name} — 1 ay",
                            Description = info.Tagline
                        }
                    }
                }
            }
        };

        // Açarı per-call ötürürük (qlobal statik state-ə güvənmirik — daha etibarlı).
        var service = new SessionService();
        Session session = await service.CreateAsync(options, new RequestOptions { ApiKey = _settings.SecretKey }, ct);
        return session.Url;
    }

    // Success səhifəsi üçün: sessiyanı Stripe-dan oxu, ödəniş "paid"-dirsə nəticəni qaytar.
    public async Task<PaidCheckout?> GetCompletedCheckoutAsync(string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return null;

        var service = new SessionService();
        Session session = await service.GetAsync(sessionId, null, new RequestOptions { ApiKey = _settings.SecretKey }, ct);
        return ToPaidCheckout(session);
    }

    // Webhook: imzanı WebhookSecret ilə yoxla, yalnız "checkout.session.completed" hadisəsini emal et.
    public PaidCheckout? ParseCompletedCheckoutWebhook(string requestJson, string stripeSignatureHeader)
    {
        // İmza səhvdirsə bu sətir StripeException atır → çağıran 400 qaytarır (saxta webhook rədd olunur).
        Event stripeEvent = EventUtility.ConstructEvent(requestJson, stripeSignatureHeader, _settings.WebhookSecret);

        if (stripeEvent.Type != "checkout.session.completed")
            return null;

        if (stripeEvent.Data.Object is not Session session)
            return null;

        return ToPaidCheckout(session);
    }

    // Ortaq köməkçi: Stripe sessiyasından (ödəniş edilibsə) PaidCheckout düzəldir.
    private static PaidCheckout? ToPaidCheckout(Session? session)
    {
        if (session is null || session.PaymentStatus != "paid")
            return null;

        string? userId =
            session.Metadata != null && session.Metadata.TryGetValue("userId", out var u) && !string.IsNullOrEmpty(u)
                ? u
                : session.ClientReferenceId;

        string? planStr = session.Metadata != null && session.Metadata.TryGetValue("plan", out var p) ? p : null;

        if (string.IsNullOrEmpty(userId) || !Enum.TryParse<SubscriptionPlan>(planStr, out var plan))
            return null;

        // PaymentReference = sessiya id-si → idempotentlik açarı (eyni ödəniş iki dəfə tətbiq olunmasın).
        return new PaidCheckout(userId, plan, session.Id);
    }

    // ---- Saytdaxili SİFARİŞ ödənişi ----

    // Sifariş üçün Checkout sessiyası: hər məhsul ayrıca line item; metadata-da type=order, orderId.
    public async Task<string> CreateOrderCheckoutUrlAsync(
        OrderCheckoutRequest request, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            throw new BusinessException("Stripe SecretKey konfiqurasiya olunmayıb — user-secrets/env yoxla və app-ı TAM yenidən başlat.");
        if (request.Lines.Count == 0)
            throw new BusinessException("Sifariş üçün məhsul yoxdur.");

        var lineItems = request.Lines.Select(l => new SessionLineItemOptions
        {
            Quantity = l.Quantity,
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = _settings.Currency,
                UnitAmount = (long)(l.UnitPrice * 100m),   // ən kiçik vahid (sent)
                ProductData = new SessionLineItemPriceDataProductDataOptions { Name = l.Name }
            }
        }).ToList();

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            PaymentMethodTypes = new List<string> { "card" },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = request.OrderId.ToString(),
            CustomerEmail = string.IsNullOrWhiteSpace(request.BuyerEmail) ? null : request.BuyerEmail,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "order",
                ["orderId"] = request.OrderId.ToString(),
                ["buyerName"] = request.BuyerName ?? string.Empty
            },
            LineItems = lineItems
        };

        var service = new SessionService();
        Session session = await service.CreateAsync(options, new RequestOptions { ApiKey = _settings.SecretKey }, ct);
        return session.Url;
    }

    // Success səhifəsi üçün: sifariş sessiyasını oxu, "paid"-dirsə nəticəni qaytar.
    public async Task<PaidOrderCheckout?> GetCompletedOrderCheckoutAsync(string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return null;
        var service = new SessionService();
        Session session = await service.GetAsync(sessionId, null, new RequestOptions { ApiKey = _settings.SecretKey }, ct);
        return ToPaidOrder(session);
    }

    // Webhook: imzanı yoxla, yalnız sifariş (type=order) ödənişini qaytar.
    public PaidOrderCheckout? ParseCompletedOrderWebhook(string requestJson, string stripeSignatureHeader)
    {
        Event stripeEvent = EventUtility.ConstructEvent(requestJson, stripeSignatureHeader, _settings.WebhookSecret);
        if (stripeEvent.Type != "checkout.session.completed") return null;
        if (stripeEvent.Data.Object is not Session session) return null;
        return ToPaidOrder(session);
    }

    // Ortaq köməkçi: Stripe sessiyasından (sifariş + ödəniş edilibsə) PaidOrderCheckout düzəldir.
    private static PaidOrderCheckout? ToPaidOrder(Session? session)
    {
        if (session is null || session.PaymentStatus != "paid")
            return null;

        // Yalnız sifariş sessiyalarını emal et (plan sessiyaları ilə qarışmasın).
        if (session.Metadata == null
            || !session.Metadata.TryGetValue("type", out var type) || type != "order")
            return null;

        string? orderIdStr = session.Metadata.TryGetValue("orderId", out var o) && !string.IsNullOrEmpty(o)
            ? o
            : session.ClientReferenceId;

        if (!int.TryParse(orderIdStr, out int orderId))
            return null;

        // PaymentReference = sessiya id-si → idempotentlik açarı.
        return new PaidOrderCheckout(orderId, session.Id);
    }
}
