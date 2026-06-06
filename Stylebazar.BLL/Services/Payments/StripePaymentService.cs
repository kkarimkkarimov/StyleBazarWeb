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
        string userId, SubscriptionPlan plan, string successUrl, string cancelUrl, CancellationToken ct = default)
    {
        if (plan == SubscriptionPlan.Free)
            throw new BusinessException("Pulsuz plan üçün ödəniş tələb olunmur.");

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
            // Webhook + success səhifəsi planı tətbiq edəndə kimi/nəyi biləcək.
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId,
                ["plan"] = plan.ToString()
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

        var service = new SessionService();
        Session session = await service.CreateAsync(options, cancellationToken: ct);
        return session.Url;
    }

    // Success səhifəsi üçün: sessiyanı Stripe-dan oxu, ödəniş "paid"-dirsə nəticəni qaytar.
    public async Task<PaidCheckout?> GetCompletedCheckoutAsync(string sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return null;

        var service = new SessionService();
        Session session = await service.GetAsync(sessionId, cancellationToken: ct);
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
}
