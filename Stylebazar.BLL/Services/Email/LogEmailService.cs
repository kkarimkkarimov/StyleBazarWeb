using Microsoft.Extensions.Logging;

namespace Stylebazar.BLL.Services.Email;

/// <summary>
/// Dev-only email "sender" — writes the message to the standard logger so we
/// can grab the URL out of the console while testing. Production replaces this
/// registration with an SMTP / SendGrid / Postmark adapter; nothing in the
/// controllers needs to change.
/// </summary>
public class LogEmailService : IEmailService
{
    private readonly ILogger<LogEmailService> _logger;

    public LogEmailService(ILogger<LogEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetAsync(string toEmail, string toName, string resetUrl, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "========== EMAIL (dev would-send) ==========\n" +
            "To:      {Email} ({Name})\n" +
            "Subject: StyleBazar — Parolun bərpası\n" +
            "Body:    Salam {NameGreeting}!\n" +
            "         Parolunu bərpa etmək üçün bu linkə klik et:\n" +
            "         {Url}\n" +
            "         Link 24 saatlıqdır — sonra yeni link tələb etməli olacaqsan.\n" +
            "============================================",
            toEmail, toName, toName, resetUrl);
        return Task.CompletedTask;
    }

    public Task SendEmailConfirmAsync(string toEmail, string toName, string confirmUrl, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "========== EMAIL (dev would-send) ==========\n" +
            "To:      {Email} ({Name})\n" +
            "Subject: StyleBazar — Email-i təsdiqlə\n" +
            "Body:    Salam {NameGreeting}!\n" +
            "         StyleBazar-a qoşulduğun üçün təşəkkür edirik.\n" +
            "         Email ünvanını təsdiqləmək üçün:\n" +
            "         {Url}\n" +
            "============================================",
            toEmail, toName, toName, confirmUrl);
        return Task.CompletedTask;
    }
}
