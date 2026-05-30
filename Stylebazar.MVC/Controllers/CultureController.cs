using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// Sets the UI culture cookie from the header language switcher. The cookie is
/// the highest-priority request-culture provider (see Program.cs), so the choice
/// sticks across requests for a year. Anonymous-friendly: language is a display
/// preference, not a privileged action.
/// </summary>
[Route("[controller]")]
public class CultureController : Controller
{
    // Mirror Program.cs supported cultures — anything else is ignored and we
    // fall back to "az" so a crafted value can never poison the cookie.
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "az", "ru", "en"
    };

    [HttpPost("Set")]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(culture) || !Allowed.Contains(culture))
            culture = "az";
        else
            culture = culture.ToLowerInvariant();

        // Persisted, HttpOnly=false (no JS reads it, but the framework's default
        // cookie is fine), 1-year expiry. RequestCulture serialized in the
        // ".AspNetCore.Culture" format the CookieRequestCultureProvider expects.
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(new CultureInfo(culture), new CultureInfo(culture))),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,           // functional cookie — exempt from consent gating
                SameSite = SameSiteMode.Lax,
                HttpOnly = false,
                Path = "/"
            });

        // LocalRedirect throws on absolute/off-site URLs — open-redirect safe.
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return Redirect("/");
    }
}
