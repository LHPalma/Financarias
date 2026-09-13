using System.Security.Cryptography;
using Financarias.Api.Security;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Financarias.Api.FunctionalTests.Security;

public class JwtAccessTokenIssuerTests
{
    private const string Issuer = "financarias";
    private const string Audience = "financarias-api";

    private static readonly DateTimeOffset Now = new(2026, 9, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly string _signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Fact(DisplayName = "O token carrega sub com o id do usuário, iss, aud e um jti")]
    public void Issue_CarriesTheExpectedClaims()
    {
        // Arrange
        var userId = Guid.CreateVersion7();

        // Act
        var jwt = Read(CreateIssuer().Issue(userId).Token);

        // Assert
        Assert.Equal(userId.ToString(), jwt.Subject);
        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Equal(Audience, Assert.Single(jwt.Audiences));
        Assert.False(string.IsNullOrWhiteSpace(jwt.Id));
    }

    [Fact(DisplayName = "O cabeçalho declara alg HS256, não a URI do XML-DSig")]
    public void Issue_DeclaresHs256InTheHeader()
    {
        // Act
        var jwt = Read(CreateIssuer().Issue(Guid.CreateVersion7()).Token);

        // Assert: com SecurityAlgorithms.HmacSha256Signature o alg viraria uma URI e o token sairia do padrão
        Assert.Equal("HS256", jwt.Alg);
        Assert.Equal("JWT", jwt.Typ);
    }

    [Fact(DisplayName = "Emissão, início de validade e expiração vêm do relógio injetado, exatos")]
    public void Issue_UsesTheInjectedClockExactly()
    {
        // Arrange
        var issuer = CreateIssuer(lifetime: TimeSpan.FromMinutes(15));

        // Act
        var issued = issuer.Issue(Guid.CreateVersion7());
        var jwt = Read(issued.Token);

        // Assert
        Assert.Equal(Now.UtcDateTime, jwt.IssuedAt);
        Assert.Equal(Now.UtcDateTime, jwt.ValidFrom);
        Assert.Equal(Now.AddMinutes(15).UtcDateTime, jwt.ValidTo);
        Assert.Equal(Now.AddMinutes(15), issued.ExpiresAt);
    }

    [Fact(DisplayName = "Cada emissão tem um jti diferente")]
    public void Issue_GeneratesAUniqueJtiPerToken()
    {
        // Arrange
        var issuer = CreateIssuer();
        var userId = Guid.CreateVersion7();

        // Act
        var first = Read(issuer.Issue(userId).Token);
        var second = Read(issuer.Issue(userId).Token);

        // Assert: o jti é o gancho da revogação — dois tokens com o mesmo jti não poderiam ser revogados separadamente
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact(DisplayName = "A assinatura confere com a chave configurada")]
    public async Task Issue_ProducesATokenThatValidatesWithTheKey()
    {
        // Arrange
        var token = CreateIssuer().Issue(Guid.CreateVersion7()).Token;

        // Act
        var result = await ValidateAsync(token, _signingKey);

        // Assert
        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact(DisplayName = "Com outra chave, a assinatura não confere")]
    public async Task Issue_ProducesATokenThatFailsWithAnotherKey()
    {
        // Arrange
        var token = CreateIssuer().Issue(Guid.CreateVersion7()).Token;
        var otherKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        // Act
        var result = await ValidateAsync(token, otherKey);

        // Assert
        Assert.False(result.IsValid);
    }

    private JwtAccessTokenIssuer CreateIssuer(TimeSpan? lifetime = null) =>
        new(
            Options.Create(new JwtOptions
            {
                Issuer = Issuer,
                Audience = Audience,
                SigningKey = _signingKey,
                AccessTokenLifetime = lifetime ?? TimeSpan.FromHours(1)
            }),
            new FakeTimeProvider(Now));

    private static JsonWebToken Read(string token) => new JsonWebTokenHandler().ReadJsonWebToken(token);

    private static Task<TokenValidationResult> ValidateAsync(string token, string signingKey) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            LifetimeValidator = (notBefore, expires, _, _) => notBefore <= Now.UtcDateTime && Now.UtcDateTime < expires
        });
}
