using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Stylebazar.BLL.Services.CurrentUser;

// CurrentUserService — xidmətin GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı). Yoxlama → repository/baza → nəticə axını burada idarə olunur.
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();
}
