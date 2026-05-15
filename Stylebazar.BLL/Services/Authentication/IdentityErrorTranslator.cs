using Microsoft.AspNetCore.Identity;

namespace Stylebazar.BLL.Services.Authentication;

/// <summary>
/// Maps ASP.NET Identity's English error codes (DuplicateEmail,
/// PasswordRequiresDigit, etc.) to user-facing Azerbaijani messages.
/// Identity ships with a <see cref="IdentityErrorDescriber"/> for this exact
/// purpose, but exposing it via DI is heavier than we need — a static map
/// keyed by <see cref="IdentityError.Code"/> is enough and stays close to the
/// auth manager that calls it.
/// </summary>
public static class IdentityErrorTranslator
{
    public static string Translate(IdentityError error) => error.Code switch
    {
        // Account
        "DuplicateUserName"            => "Bu email artıq qeydiyyatdadır.",
        "DuplicateEmail"               => "Bu email artıq qeydiyyatdadır.",
        "InvalidUserName"              => "İstifadəçi adı yalnız hərf və rəqəm içerə bilər.",
        "InvalidEmail"                 => "Email düzgün formatda deyil.",
        "UserAlreadyHasPassword"       => "İstifadəçinin artıq parolu var.",
        "UserLockoutNotEnabled"        => "İstifadəçi üçün kilid funksiyası deaktivdir.",
        "UserAlreadyInRole"            => "İstifadəçi artıq bu rolda yer alır.",
        "UserNotInRole"                => "İstifadəçi bu rolda deyil.",

        // Password — keep these in sync with PasswordOptions in
        // ServiceRegistration.AddIdentityServices().
        "PasswordTooShort"             => "Parol minimum 6 simvol olmalıdır.",
        "PasswordRequiresUniqueChars"  => "Parol fərqli simvollar içerməlidir.",
        "PasswordRequiresNonAlphanumeric" => "Parol ən az 1 xüsusi simvol içerməlidir (məs: !@#).",
        "PasswordRequiresDigit"        => "Parol ən az 1 rəqəm içerməlidir.",
        "PasswordRequiresLower"        => "Parol ən az 1 kiçik hərf içerməlidir.",
        "PasswordRequiresUpper"        => "Parol ən az 1 böyük hərf içerməlidir.",
        "PasswordMismatch"             => "Mövcud parol səhvdir.",

        // Token / 2FA — used by password reset, email confirmation
        "InvalidToken"                 => "Təsdiq linkinin müddəti bitib və ya etibarsızdır.",
        "RecoveryCodeRedemptionFailed" => "Bərpa kodu səhvdir.",
        "LoginAlreadyAssociated"       => "Bu hesab artıq başqa bir profilə bağlıdır.",

        // Role
        "InvalidRoleName"              => "Rol adı düzgün deyil.",
        "DuplicateRoleName"            => "Bu adda rol artıq mövcuddur.",
        "RoleNotFound"                 => "Rol tapılmadı.",

        // Fallback — Identity ships descriptions in English; we'd rather
        // show a single clean Az sentence than leak the system message.
        _ => "Hesab əməliyyatında xəta baş verdi. Yenidən cəhd et."
    };

    /// <summary>
    /// Joins all errors in an <see cref="IdentityResult"/> as a single
    /// user-facing Az string, separated by " • ".
    /// </summary>
    public static string Translate(IdentityResult result) =>
        string.Join(" • ", result.Errors.Select(Translate).Distinct());
}
