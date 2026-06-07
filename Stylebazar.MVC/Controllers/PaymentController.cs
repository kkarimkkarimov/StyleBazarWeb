using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Stylebazar.BLL.Services.Payments;
using Stylebazar.BLL.Services.Subscriptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.MVC.Controllers;

// PaymentController — kartla aylıq paket alışını idarə edir (Stripe Checkout).
// Axın: Checkout (sessiya yarat → Stripe-a yönəlt) → istifadəçi ödəyir → Stripe Success-ə qaytarır
// + Webhook göndərir. Plan həm Success səhifəsində, həm Webhook-da tətbiq olunur (idempotent).
[Authorize]
public class PaymentController : Controller
{
    private const int MonthsPerPurchase = 1;   // birdəfəlik alış = 1 ay

    private readonly IPaymentService _payments;
    private readonly ISubscriptionService _subscriptions;
    private readonly UserManager<AppUser> _users;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IPaymentService payments,
        ISubscriptionService subscriptions,
        UserManager<AppUser> users,
        ILogger<PaymentController> logger)
    {
        _payments = payments;
        _subscriptions = subscriptions;
        _users = users;
        _logger = logger;
    }

    // Satıcı "Pro al / VIP al" düyməsinə basır → Stripe Checkout sessiyası yarat → Stripe-a yönəlt.
    // Satıcı "Kartla al" düyməsinə basır → AJAX ilə çağırılır → Stripe Checkout sessiyası yaradılır
    // və session URL-i JSON kimi qaytarılır. Brauzer JS həmin URL-ə window.location ilə keçir.
    // (JSON qaytarmaq form-submit + cross-origin 302 tələlərini aradan qaldırır; xəta aydın görünür.)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("write-default")]
    public async Task<IActionResult> Checkout(SubscriptionPlan plan, CancellationToken ct)
    {
        if (plan == SubscriptionPlan.Free)
            return BadRequest(new { ok = false, error = "Pulsuz plan üçün ödəniş tələb olunmur." });

        AppUser? user = await _users.GetUserAsync(User);
        if (user is null)
            return Unauthorized(new { ok = false, error = "Bu əməliyyat üçün hesabınıza daxil olun." });

        // Stripe geri qayıtma ünvanları (mütləq/absolute olmalıdır).
        string successUrl = Url.Action(nameof(Success), "Payment", null, Request.Scheme)!
                            + "?session_id={CHECKOUT_SESSION_ID}";
        string cancelUrl = Url.Action(nameof(Cancel), "Payment", null, Request.Scheme)!;

        try
        {
            // Satıcının email + adını da ötürürük ki, Stripe dashboard-da kimin ödədiyi aydın görünsün.
            string url = await _payments.CreateCheckoutUrlAsync(
                user.Id, user.Email, user.FullName, plan, successUrl, cancelUrl, ct);
            return Json(new { ok = true, url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe checkout session yaradıla bilmədi (plan {Plan})", plan);
            return StatusCode(500, new { ok = false, error = "Ödəniş başlana bilmədi: " + ex.Message });
        }
    }

    // Stripe uğurlu ödənişdən sonra bura qaytarır. Planı tətbiq edirik (idempotent — webhook da tətbiq edir).
    // Qeyd: ödənişin HƏQİQİ təsdiqi Stripe-dan oxunan sessiyadır (paymentStatus=paid), URL-ə güvənmirik.
    [HttpGet]
    public async Task<IActionResult> Success(string? session_id, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(session_id))
        {
            try
            {
                PaidCheckout? paid = await _payments.GetCompletedCheckoutAsync(session_id, ct);
                if (paid is not null)
                {
                    await _subscriptions.ApplyPaidPlanAsync(paid.UserId, paid.Plan, MonthsPerPurchase, paid.PaymentReference, ct);
                    TempData["Success"] = "Ödəniş uğurlu oldu! Planın aktivləşdirildi. 🎉";
                    return RedirectToAction("Packages", "Seller");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Success səhifəsində plan tətbiqi alınmadı (session {Session})", session_id);
            }
        }

        // Ödəniş hələ təsdiqlənməyibsə (məs. webhook gecikir) — istifadəçiyə bildir.
        TempData["Success"] = "Ödəniş alındı. Plan bir neçə saniyəyə aktivləşəcək.";
        return RedirectToAction("Packages", "Seller");
    }

    // İstifadəçi Stripe-da ödənişi ləğv etdi.
    [HttpGet]
    public IActionResult Cancel()
    {
        TempData["Error"] = "Ödəniş ləğv edildi.";
        return RedirectToAction("Packages", "Seller");
    }

    // STRIPE WEBHOOK — ödənişin ETİBARLI mənbəyi. Stripe serveri çağırır (anonim), ona görə [AllowAnonymous]
    // + antiforgery yoxdur; əvəzində imza WebhookSecret ilə yoxlanır (saxta sorğuya qarşı qoruma).
    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Webhook(CancellationToken ct)
    {
        string json = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        string signature = Request.Headers["Stripe-Signature"].ToString();

        try
        {
            PaidCheckout? paid = _payments.ParseCompletedCheckoutWebhook(json, signature);
            if (paid is not null)
                await _subscriptions.ApplyPaidPlanAsync(paid.UserId, paid.Plan, MonthsPerPurchase, paid.PaymentReference, ct);

            return Ok();   // Stripe 2xx görməlidir, yoxsa təkrar göndərər
        }
        catch (Exception ex)
        {
            // İmza səhvi və ya emal xətası → 400; Stripe sonra yenidən cəhd edəcək.
            _logger.LogWarning(ex, "Stripe webhook rədd edildi (imza və ya emal xətası).");
            return BadRequest();
        }
    }
}
