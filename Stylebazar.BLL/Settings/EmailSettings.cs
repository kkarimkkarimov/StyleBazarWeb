namespace Stylebazar.BLL.Settings;

/// <summary>
/// Email transport configuration. Mapped from the "Email" section.
/// Dev (no section configured) → Provider defaults to "Log" and
/// LogEmailService is registered. Prod sets Provider="Smtp" plus the
/// Host/Port/Username/Password fields and SmtpEmailService picks up.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>"Log" (dev, default) or "Smtp" (prod).</summary>
    public string Provider { get; set; } = "Log";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;

    /// <summary>SMTP auth username. Often the same as FromAddress.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>SMTP auth password. ALWAYS via env var Email__Password in prod.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Visible "From" address on outgoing mail (must match SMTP creds for most providers).</summary>
    public string FromAddress { get; set; } = "noreply@stylebazar.az";

    /// <summary>Human-friendly From name shown in the recipient's inbox.</summary>
    public string FromName { get; set; } = "StyleBazar";

    /// <summary>STARTTLS / TLS — every modern SMTP needs this on, default true.</summary>
    public bool UseTls { get; set; } = true;
}
