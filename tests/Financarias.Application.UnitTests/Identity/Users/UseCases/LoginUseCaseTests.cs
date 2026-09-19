using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.UseCases;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Identity;
using NSubstitute;

namespace Financarias.Application.UnitTests.Identity.Users.UseCases;

public class LoginUseCaseTests
{
    private static readonly IssuedAccessToken Issued =
        new("token-emitido", new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));

    private readonly ICommandHandler<LoginCommand, IssuedAccessToken> _handler =
        Substitute.For<ICommandHandler<LoginCommand, IssuedAccessToken>>();

    [Fact(DisplayName = "Monta o comando com o e-mail normalizado e a senha exatamente como veio")]
    public async Task ExecuteAsync_BuildsCommand_WithNormalizedEmailAndRawPassword()
    {
        // Arrange
        HandlerReturns(Issued);

        // Act
        await CreateUseCase().ExecuteAsync(new LoginRequest("  Luiz@Example.COM  ", " S3nha-Forte! "));

        // Assert: a senha não é aparada nem validada — login confere credencial, não regra de cadastro
        await _handler.Received(1).HandleAsync(
            Arg.Is<LoginCommand>(c => c.Email.Value == "luiz@example.com" && c.Password == " S3nha-Forte! "),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Mapeia o token emitido para AccessTokenResult")]
    public async Task ExecuteAsync_MapsIssuedTokenToResult()
    {
        // Arrange
        HandlerReturns(Issued);

        // Act
        var result = await CreateUseCase().ExecuteAsync(new LoginRequest("luiz@example.com", "S3nha-Forte!"));

        // Assert
        Assert.Equal(Issued.Token, result.AccessToken);
        Assert.Equal(Issued.ExpiresAt, result.ExpiresAt);
    }

    [Theory(DisplayName = "E-mail malformado lança identity.credentials.invalid sem chamar o handler")]
    [InlineData("nao-e-email")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_MalformedEmail_ThrowsInvalidCredentials(string email)
    {
        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateUseCase().ExecuteAsync(new LoginRequest(email, "S3nha-Forte!")));

        // Assert: o mesmo código de qualquer outra falha (RN-18), e não contacts.email.invalid,
        // que diria ao chamador que o problema foi o formato do e-mail
        Assert.Equal("identity.credentials.invalid", exception.Code);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A falha do handler propaga sem ser traduzida")]
    public async Task ExecuteAsync_PropagatesHandlerFailure()
    {
        // Arrange
        _handler
            .HandleAsync(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns<IssuedAccessToken>(_ => throw IdentityErrors.InvalidCredentials());

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateUseCase().ExecuteAsync(new LoginRequest("luiz@example.com", "errada")));

        // Assert
        Assert.Equal("identity.credentials.invalid", exception.Code);
    }

    private LoginUseCase CreateUseCase() => new(_handler);

    private void HandlerReturns(IssuedAccessToken token) =>
        _handler
            .HandleAsync(Arg.Any<LoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(token);
}
