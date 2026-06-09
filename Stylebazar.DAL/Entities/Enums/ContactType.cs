// Enum-ların yerləşdiyi ad fəzası.
namespace Stylebazar.DAL.Entities.Enums;

// ƏLAQƏ KANALI enum-u — alıcı hansı kanala kliklədi? Satıcı/admin analitikasını qidalandırır
// (məs. "əlaqələrin 60%-i WhatsApp ilə gəlib").
public enum ContactType
{
    WhatsApp = 0,   // WhatsApp.
    Instagram = 1,  // Instagram.
    Telegram = 2,   // Telegram.
    Phone = 3,      // Telefon zəngi.
    Facebook = 4,   // Facebook.
    TikTok = 5,     // TikTok.
    Email = 6       // Email.
}
