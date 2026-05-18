namespace Stylebazar.BLL.Services.Email;

/// <summary>
/// Thin abstraction over outbound transactional email. The MVP uses
/// <see cref="LogEmailService"/> which writes the message to ILogger;
/// production deployments swap in an SMTP / SendGrid / Postmark impl
/// without touching the controllers.
/// </summary>
public interface IEmailService
{
    /// <summary>Password-reset email with a one-shot URL. Token already encoded.</summary>
    Task SendPasswordResetAsync(string toEmail, string toName, string resetUrl, CancellationToken ct = default);

    /// <summary>Email-confirmation link sent right after registration (when enabled).</summary>
    Task SendEmailConfirmAsync(string toEmail, string toName, string confirmUrl, CancellationToken ct = default);
}
