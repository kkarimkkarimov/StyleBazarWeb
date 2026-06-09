// Enum-ların yerləşdiyi ad fəzası.
namespace Stylebazar.DAL.Entities.Enums;

// GİRİŞ HADİSƏSİNİN NÖVÜ enum-u. LoginEvent sətrinin hansı autentifikasiya hadisəsini qeyd
// etdiyini bildirir. Giriş tarixçəsindəki nişanın rəngini və etiketini idarə edir.
public enum LoginEventType
{
    SignUp = 0,             // İlk dəfə qeydiyyat.
    SignIn = 1,             // Uğurlu giriş.
    SignOut = 2,            // Əl ilə çıxış.
    FailedSignIn = 3,       // Səhv parol (uğursuz giriş).
    PasswordChanged = 4,    // Parolun dəyişdirilməsi.
    PasswordReset = 5,      // Email token ilə parol bərpası.
    EmailVerified = 6,      // Email təsdiqi.
    TwoFactorChallenge = 7, // 2FA sorğusu (gələcək üçün).
    Locked = 8              // Hesab kilidləndi (5 ardıcıl uğursuz cəhd).
}
