namespace Stylebazar.BLL.Settings;

// STRIPE AYARLARI — appsettings "Stripe" bölməsindən + user-secrets/env-dən oxunur.
public class StripeSettings
{
    public const string SectionName = "Stripe";

    // Public açar (pk_...) — brauzerdə görünə bilər, gizli deyil.
    public string PublishableKey { get; set; } = string.Empty;

    // GİZLİ açar (sk_...) — yalnız server-də qalmalıdır; heç vaxt brauzerə və ya git-ə düşməməlidir.
    public string SecretKey { get; set; } = string.Empty;

    // Webhook imza sirri (whsec_...) — Stripe-dan gələn webhook-un həqiqiliyini yoxlamaq üçün.
    public string WebhookSecret { get; set; } = string.Empty;

    // Ödəniş valyutası (Stripe formatı: kiçik hərflə, məs. "usd").
    public string Currency { get; set; } = "usd";
}
