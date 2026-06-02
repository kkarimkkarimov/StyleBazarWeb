using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Stylebazar.BLL.Settings;
using Stylebazar.DAL.Entities;

namespace Stylebazar.BLL.Services.Authentication;

// TokenService — xidmətin GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı). Yoxlama → repository/baza → nəticə axını burada idarə olunur.
public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public TokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public (string token, DateTime expiresAt) CreateToken(AppUser user, IList<string> roles)
    {
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpireMinutes);

        List<Claim> claims = new()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        foreach (string role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_settings.Key));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken jwt = new(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: creds);

        string tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (tokenString, expiresAt);
    }
}
