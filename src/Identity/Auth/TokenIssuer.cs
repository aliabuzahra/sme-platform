using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Sme.BuildingBlocks.Auth;

namespace Sme.Identity.Auth;

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);

/// <summary>Signs platform access tokens. Claims are the ones in <see cref="PlatformClaims"/>.</summary>
public sealed class TokenIssuer(JwtOptions options, TimeProvider clock)
{
    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)), SecurityAlgorithms.HmacSha256);

    public IssuedToken Issue(Guid userId, Guid tenantId, string phone, string role, IReadOnlyList<string> modules)
    {
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(options.LifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(PlatformClaims.TenantId, tenantId.ToString()),
            new(PlatformClaims.Phone, phone),
            new(PlatformClaims.Role, role),
        };
        claims.AddRange(modules.Select(m => new Claim(PlatformClaims.Modules, m)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = _credentials,
        };
        return new IssuedToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
