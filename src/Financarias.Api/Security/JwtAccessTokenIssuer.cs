using Financarias.Application.Common.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Financarias.Api.Security;

public sealed class JwtAccessTokenIssuer(
    IOptions<JwtOptions> options,
    TimeProvider timeProvider
) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public IssuedAccessToken Issue(Guid userId)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(jwt.AccessTokenLifetime);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = userId.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString(),
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256),
        });

        return new IssuedAccessToken(token, expiresAt);
    }
}