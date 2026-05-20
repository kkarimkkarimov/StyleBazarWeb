using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Stylebazar.BLL.Settings;

namespace Stylebazar.BLL.Services.Email;

/// <summary>
/// Real SMTP sender backed by MailKit. Selected automatically when
/// EmailSettings.Provider == "Smtp" (production wiring). Connection
/// open/close happens per send — adequate for low-volume transactional
/// traffic (password reset, email confirmation). A pooled SmtpClient
/// could replace this if we ever fan out marketing email.
///
/// Auth failures and connection errors are LOGGED but swallowed so a
/// dead SMTP relay can't 500 a user's password-reset flow — the
/// AccountController returns a generic "if your email exists we sent
/// it" message anyway (no-enumeration), so a silently-failed send
/// degrades to that same UX. Operators see the failure in logs and
/// can swap providers without forcing user-facing errors.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task SendPasswordResetAsync(string toEmail, string toName, string resetUrl, CancellationToken ct = default)
    {
        string subject = "StyleBazar — Parolun bərpası";
        string html = BuildHtml(
            heading: "Parolunu bərpa et",
            greeting: $"Salam {Html(toName)}!",
            body:
                "Parolunu bərpa etmək üçün aşağıdakı düyməyə klik et — link <strong>24 saatlıqdır</strong>.",
            ctaText: "Parolu bərpa et",
            ctaUrl: resetUrl,
            footer:
                "Əgər bu sorğunu sən etməmisənsə, bu emaili dikkətsiz nəzər salıb sil. " +
                "Parolun dəyişmir, hesabın da təhlükəsizdir.");
        return SendAsync(toEmail, toName, subject, html, ct);
    }

    public Task SendEmailConfirmAsync(string toEmail, string toName, string confirmUrl, CancellationToken ct = default)
    {
        string subject = "StyleBazar — Email-i təsdiqlə";
        string html = BuildHtml(
            heading: "Email ünvanını təsdiqlə",
            greeting: $"Salam {Html(toName)}!",
            body: "StyleBazar-a qoşulduğun üçün təşəkkür edirik. Email ünvanını təsdiqlə və alış-verişə başla.",
            ctaText: "Email-i təsdiqlə",
            ctaUrl: confirmUrl,
            footer: "Əgər StyleBazar hesabı yaratmamısansa, bu emaili sadəcə sil.");
        return SendAsync(toEmail, toName, subject, html, ct);
    }

    // ====== internals ======

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        msg.To.Add(new MailboxAddress(toName, toEmail));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        try
        {
            using var client = new SmtpClient();
            var socketOption = _settings.UseTls
                ? SecureSocketOptions.StartTlsWhenAvailable
                : SecureSocketOptions.None;

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOption, ct);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);
            }

            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(quit: true, ct);

            _logger.LogInformation("Email sent to {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            // Swallow so password-reset endpoints don't leak SMTP state to
            // callers (no-enumeration principle). Operations see the
            // failure in structured logs.
            _logger.LogError(ex, "SMTP send to {Email} ({Subject}) failed", toEmail, subject);
        }
    }

    private static string BuildHtml(string heading, string greeting, string body, string ctaText, string ctaUrl, string footer)
    {
        // Inline-styled HTML — Gmail / Outlook strip <style> blocks
        // aggressively. Single <table> layout for legacy client safety.
        return $@"<!doctype html>
<html lang=""az""><body style=""margin:0;padding:0;background:#F1F5F9;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#0F172A;"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0""><tr><td align=""center"" style=""padding:32px 16px;"">
  <table width=""560"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""max-width:560px;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(15,23,42,.06);"">
    <tr><td style=""padding:32px 32px 16px;"">
      <div style=""font-family:Playfair Display,Georgia,serif;font-size:1.6rem;font-weight:700;color:#0F172A;letter-spacing:-.01em;"">StyleBazar<span style=""color:#0EA5B7;"">.</span></div>
    </td></tr>
    <tr><td style=""padding:0 32px 8px;"">
      <h1 style=""margin:0;font-size:1.4rem;font-weight:600;color:#0F172A;line-height:1.3;"">{Html(heading)}</h1>
    </td></tr>
    <tr><td style=""padding:8px 32px 24px;color:#475569;line-height:1.55;font-size:.95rem;"">
      <p style=""margin:0 0 12px;"">{greeting}</p>
      <p style=""margin:0 0 24px;"">{body}</p>
      <div style=""text-align:center;margin:24px 0;"">
        <a href=""{Html(ctaUrl)}"" style=""display:inline-block;padding:14px 28px;background:#0F172A;color:#ffffff;text-decoration:none;border-radius:999px;font-weight:600;font-size:.95rem;"">{Html(ctaText)}</a>
      </div>
      <p style=""margin:24px 0 0;font-size:.82rem;color:#94A3B8;line-height:1.5;"">{footer}</p>
    </td></tr>
    <tr><td style=""padding:16px 32px;background:#F8FAFC;border-top:1px solid #E2E8F0;font-size:.78rem;color:#94A3B8;text-align:center;"">
      © {DateTime.UtcNow.Year} StyleBazar — Bakının yerli moda marketplace-i
    </td></tr>
  </table>
</td></tr></table>
</body></html>";
    }

    private static string Html(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
