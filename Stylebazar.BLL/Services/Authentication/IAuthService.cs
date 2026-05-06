using Microsoft.AspNetCore.Identity;
using Stylebazar.BLL.DTOs.Authentication;

namespace Stylebazar.BLL.Services.Authentication;

public interface IAuthService
{
    Task<IdentityResult> RegisterAsync(RegisterDto dto);
    Task<LoginResultDto> LoginAsync(LoginDto dto);
    Task LogoutAsync();
}
