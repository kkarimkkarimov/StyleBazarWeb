using Stylebazar.BLL.DTOs.Authentication;

namespace Stylebazar.BLL.Services.Authentication;

// IAuthService — bu xidmətin MÜQAVİLƏSİ (interfeys). Hansı əməliyyatların mövcud olacağını təyin edir; gerçəkləşmə ayrı sinifdədir.
public interface IAuthService
{
    /// <summary>
    /// Creates a new user account, assigns the chosen role, returns a structured
    /// <see cref="RegisterResultDto"/>. Validation + Identity errors are surfaced
    /// as field-keyed messages — controllers should never need to catch
    /// exceptions to render a clean form.
    /// </summary>
    Task<RegisterResultDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);

    /// <summary>JWT-emitting login for the API. MVC uses SignInManager directly.</summary>
    Task<LoginResultDto> LoginAsync(LoginDto dto);

    Task LogoutAsync();
}
