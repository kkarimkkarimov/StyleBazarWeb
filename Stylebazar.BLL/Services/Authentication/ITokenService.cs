using Stylebazar.DAL.Entities;

namespace Stylebazar.BLL.Services.Authentication;

// ITokenService — bu xidmətin MÜQAVİLƏSİ (interfeys). Hansı əməliyyatların mövcud olacağını təyin edir; gerçəkləşmə ayrı sinifdədir.
public interface ITokenService
{
    (string token, DateTime expiresAt) CreateToken(AppUser user, IList<string> roles);
}
