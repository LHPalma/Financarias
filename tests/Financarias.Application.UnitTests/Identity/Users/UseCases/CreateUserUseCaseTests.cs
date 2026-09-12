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

public class CreateUserUseCaseTests
{
    private const string StrongPassword = "S3nha-Forte!";

    private readonly ICommandHandler<CreateUserCommand, User> _handler =
        Substitute.For<ICommandHandler<CreateUserCommand, User>>();

    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    public CreateUserUseCaseTests()
    {
        _hasher.Hash(Arg.Any<Password>()).Returns(TestPasswordHashes.Any);
    }

    [Fact(DisplayName = "Monta o comando com o e-mail normalizado e o hash devolvido pela porta")]
    public async Task ExecuteAsync_BuildsCommandWithEmailAndHash()
    {
        // Arrange
        HandlerReturns(User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any));

        // Act
        await CreateUseCase().ExecuteAsync(new CreateUserRequest("Luiz Palma", "  Luiz@Example.COM  ", StrongPassword));

        // Assert
        await _handler.Received(1).HandleAsync(
            Arg.Is<CreateUserCommand>(c =>
                c.Name == "Luiz Palma"
                && c.Email.Value == "luiz@example.com"
                && c.PasswordHash == TestPasswordHashes.Any),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Hasheia a senha já validada, sem alterá-la")]
    public async Task ExecuteAsync_HashesTheValidatedPassword()
    {
        // Arrange
        HandlerReturns(User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any));

        // Act
        await CreateUseCase().ExecuteAsync(new CreateUserRequest("Luiz Palma", "luiz@example.com", StrongPassword));

        // Assert
        _hasher.Received(1).Hash(Arg.Is<Password>(p => p.Value == StrongPassword));
    }

    [Fact(DisplayName = "Mapeia o agregado para UserResult, achatando o VO de e-mail")]
    public async Task ExecuteAsync_MapsAggregateToResult()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any);
        HandlerReturns(user);

        // Act
        var result = await CreateUseCase().ExecuteAsync(new CreateUserRequest("Luiz Palma", "luiz@example.com", StrongPassword));

        // Assert
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("Luiz Palma", result.Name);
        Assert.Equal("luiz@example.com", result.Email);
        Assert.Equal(UserStatus.Active, result.Status);
    }

    [Fact(DisplayName = "E-mail inválido lança antes de hashear e antes de chamar o handler")]
    public async Task ExecuteAsync_InvalidEmail_ThrowsBeforeHashingOrHandling()
    {
        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateUseCase().ExecuteAsync(new CreateUserRequest("Luiz Palma", "nao-e-email", StrongPassword)));

        // Assert
        Assert.Equal("contacts.email.invalid", exception.Code);
        _hasher.DidNotReceive().Hash(Arg.Any<Password>());
        await _handler.DidNotReceive().HandleAsync(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "Senha fora da política lança o código da regra antes de hashear")]
    [InlineData("curta1!", "identity.password.tooshort")]
    [InlineData("semmaiuscula1!", "identity.password.missinguppercase")]
    [InlineData("SemDigito!", "identity.password.missingdigit")]
    public async Task ExecuteAsync_WeakPassword_ThrowsBeforeHashing(string password, string expectedCode)
    {
        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateUseCase().ExecuteAsync(new CreateUserRequest("Luiz Palma", "luiz@example.com", password)));

        // Assert: o Argon2 custa ~150 ms, e senha que não passa na política nem chega nele
        Assert.Equal(expectedCode, exception.Code);
        _hasher.DidNotReceive().Hash(Arg.Any<Password>());
        await _handler.DidNotReceive().HandleAsync(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>());
    }

    private CreateUserUseCase CreateUseCase() => new(_handler, _hasher, new UserMapper());

    private void HandlerReturns(User user) =>
        _handler
            .HandleAsync(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(user);
}
