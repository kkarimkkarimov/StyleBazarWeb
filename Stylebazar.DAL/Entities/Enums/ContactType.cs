namespace Stylebazar.DAL.Entities.Enums;

/// <summary>
/// Which contact channel the buyer clicked. Drives analytics breakdown
/// for sellers and admin (e.g. "60% of contacts came via WhatsApp").
/// </summary>
public enum ContactType
{
    WhatsApp = 0,
    Instagram = 1,
    Telegram = 2,
    Phone = 3,
    Facebook = 4,
    TikTok = 5,
    Email = 6
}
