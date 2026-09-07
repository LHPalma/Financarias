using Financarias.Application.Common.Messaging;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Application.Identity.Users.Mappers;
using Financarias.Application.Identity.Users.UseCases;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using NSubstitute;

namespace Financarias.Application.UnitTests.Identity.Users.UseCases;

public class CreateUserUseCaseTests
{
    private readonly ICommandHandler<CreateUserCommand, User> _handler =
        Substitute.For<ICommandHandler<CreateUserCommand, User>>();

    [Fact(DisplayName = "Monta o comando com o e-mail já normalizado em VO e delega ao handler")]
    public async Task ExecuteAsync_BuildsCommandWithEmailValueObject()
    {
        // Arrange
        HandlerReturns(User.Create("Luiz Palma", Email.Create("luiz@example.com")));

        // Act
        await CreateUseCase().ExecuteAsync("Luiz Palma", "  Luiz@Example.COM  ");

        // Assert
        await _handler.Received(1).HandleAsync(
            Arg.Is<CreateUserCommand>(c => c.Name == "Luiz Palma" && c.Email.Value == "luiz@example.com"),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Mapeia o agregado para UserResult, achatando o VO de e-mail")]
    public async Task ExecuteAsync_MapsAggregateToResult()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"));
        HandlerReturns(user);

        // Act
        var result = await CreateUseCase().ExecuteAsync("Luiz Palma", "luiz@example.com");

        // Assert
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("Luiz Palma", result.Name);
        Assert.Equal("luiz@example.com", result.Email);
        Assert.Equal(UserStatus.Active, result.Status);
    }

    [Fact(DisplayName = "E-mail inválido lança antes de o handler ser chamado")]
    public async Task ExecuteAsync_Throws_BeforeReachingTheHandler()
    {
        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateUseCase().ExecuteAsync("Luiz Palma", "nao-e-email"));

        // Assert
        Assert.Equal("contacts.email.invalid", exception.Code);

        await _handler.DidNotReceive().HandleAsync(
            Arg.Any<CreateUserCommand>(),
            Arg.Any<CancellationToken>());
    }

    private CreateUserUseCase CreateUseCase() => new(_handler, new UserMapper());

    private void HandlerReturns(User user) =>
        _handler
            .HandleAsync(Arg.Any<CreateUserCommand>(), Arg.Any<CancellationToken>())
            .Returns(user);
}
