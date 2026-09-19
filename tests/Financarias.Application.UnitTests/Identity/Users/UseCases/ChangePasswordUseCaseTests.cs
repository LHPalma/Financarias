using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.Mappers;
using Financarias.Application.Identity.Users.UseCases;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using NSubstitute;

namespace Financarias.Application.UnitTests.Identity.Users.UseCases;

public class ChangePasswordUseCaseTests
{
    private const string CurrentSecret = "Atual-Senha1!";
    private const string NewSecret = "Nova-Senha2@";

    private readonly ICommandHandler<ChangePasswordCommand, User> _handler =
        Substitute.For<ICommandHandler<ChangePasswordCommand, User>>();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    [Fact(DisplayName = "Monta o comando com o id do usuário corrente, a senha atual crua e a nova já validada")]
    public async Task ExecuteAsync_BuildsCommand_FromTheCurrentUser()
    {
        // Arrange
        var user = CreateUser();
        _currentUser.UserId.Returns(user.Id);
        HandlerReturns(user);

        // Act
        await CreateUseCase().ExecuteAsync(new ChangePasswordRequest(CurrentSecret, NewSecret));

        // Assert: a senha atual não passa pela política — ela só precisa conferir com o hash gravado
        await _handler.Received(1).HandleAsync(
            Arg.Is<ChangePasswordCommand>(c =>
                c.UserId == user.Id
                && c.CurrentPassword == CurrentSecret
                && c.NewPassword.Value == NewSecret),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Mapeia o usuário para UserResult")]
    public async Task ExecuteAsync_MapsUserToResult()
    {
        // Arrange
        var user = CreateUser();
        _currentUser.UserId.Returns(user.Id);
        HandlerReturns(user);

        // Act
        var result = await CreateUseCase().ExecuteAsync(new ChangePasswordRequest(CurrentSecret, NewSecret));

        // Assert
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("luiz@example.com", result.Email);
    }

    [Theory(DisplayName = "Senha nova fora da política lança o código da regra sem chamar o handler")]
    [InlineData("curta1!", "identity.password.tooshort")]
    [InlineData("semmaiuscula1!", "identity.password.missinguppercase")]
    [InlineData("SemDigito!", "identity.password.missingdigit")]
    public async Task ExecuteAsync_WeakNewPassword_ThrowsBeforeHandling(string newPassword, string expectedCode)
    {
        // Arrange
        _currentUser.UserId.Returns(Guid.CreateVersion7());

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateUseCase().ExecuteAsync(new ChangePasswordRequest(CurrentSecret, newPassword)));

        // Assert: nem consulta o banco nem gasta Argon2 com uma senha que não passa na política
        Assert.Equal(expectedCode, exception.Code);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ChangePasswordCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Sem usuário corrente lança InvalidOperationException, não um erro de domínio")]
    public async Task ExecuteAsync_Throws_WithoutCurrentUser()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act & Assert: só acontece se um resolver ficar sem [Authorize] — é erro de programação
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateUseCase().ExecuteAsync(new ChangePasswordRequest(CurrentSecret, NewSecret)));

        await _handler.DidNotReceive().HandleAsync(Arg.Any<ChangePasswordCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "A falha do handler propaga sem ser traduzida")]
    public async Task ExecuteAsync_PropagatesHandlerFailure()
    {
        // Arrange
        _currentUser.UserId.Returns(Guid.CreateVersion7());
        _handler
            .HandleAsync(Arg.Any<ChangePasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns<User>(_ => throw IdentityErrors.IncorrectCurrentPassword());

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateUseCase().ExecuteAsync(new ChangePasswordRequest("Errada-Senha1!", NewSecret)));

        // Assert
        Assert.Equal("identity.password.currentincorrect", exception.Code);
    }

    private ChangePasswordUseCase CreateUseCase() => new(_handler, _currentUser, new UserMapper());

    private static User CreateUser() =>
        User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any);

    private void HandlerReturns(User user) =>
        _handler
            .HandleAsync(Arg.Any<ChangePasswordCommand>(), Arg.Any<CancellationToken>())
            .Returns(user);
}
