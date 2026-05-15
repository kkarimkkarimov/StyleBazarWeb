namespace Stylebazar.BLL.DTOs.Authentication;

/// <summary>
/// Outcome of a registration attempt — keeps the controller off of mixed
/// IdentityResult-or-throw signatures. Either <see cref="UserId"/> is filled
/// in (success) or one of <see cref="FieldErrors"/> / <see cref="GeneralError"/>
/// is populated. Never both.
/// </summary>
public class RegisterResultDto
{
    public bool Succeeded { get; init; }

    /// <summary>The newly-created user's id when <see cref="Succeeded"/> is true.</summary>
    public string? UserId { get; init; }

    /// <summary>Az-language email for the welcome flow (e.g. "Xoş gəldin, Ayşən!").</summary>
    public string? Email { get; init; }

    /// <summary>The first part of FullName, safe against empty strings — used in TempData greetings.</summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Field-level errors, e.g. { "Email": ["Bu email artıq qeydiyyatdadır."] }.
    /// The controller relays these straight into ModelState so each error shows
    /// next to its input — no more generic "validation failed" messages.
    /// </summary>
    public Dictionary<string, string[]> FieldErrors { get; init; } = new();

    /// <summary>Form-level error when there's no good field to attribute to (e.g. database write failure).</summary>
    public string? GeneralError { get; init; }
}
