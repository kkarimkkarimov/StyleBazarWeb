using Stylebazar.DAL.Entities;

namespace Stylebazar.BLL.Services.Authentication;

public interface ITokenService
{
    (string token, DateTime expiresAt) CreateToken(AppUser user, IList<string> roles);
}
