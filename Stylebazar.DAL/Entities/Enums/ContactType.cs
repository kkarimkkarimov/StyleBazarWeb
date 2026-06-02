namespace Stylebazar.DAL.Entities.Enums;

// ƏLAQƏ KANALI enum-u — alıcı hansı kanala kliklədi? Satıcı/admin analitikasını qidalandırır
// (məs. "əlaqələrin 60%-i WhatsApp ilə gəlib").
public enum ContactType
{
    WhatsApp = 0,
    Instagram = 1,
    Telegram = 2,
    Phone = 3,      // Telefon
    Facebook = 4,
    TikTok = 5,
    Email = 6
}
