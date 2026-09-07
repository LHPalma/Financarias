using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using NSubstitute;

namespace Financarias.Application.UnitTests.Identity.Users.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly IRepository<User> _repository = Substitute.For<IRepository<User>>();

    [Fact(DisplayName = "Cria o usuário e persiste quando o e-mail ainda não está em uso")]
    public async Task HandleAsync_CreatesUser_WhenEmailIsFree()
    {
        // Arrange
        var email = Email.Create("livre@example.com");
        RepositoryFinds(null);

        // Act
        var user = await new CreateUserCommandHandler(_repository)
            .HandleAsync(new CreateUserCommand("Luiz Palma", email));

        // Assert
        Assert.Equal("Luiz Palma", user.Name);
        Assert.Equal(email, user.Email);
        Assert.Equal(UserStatus.Active, user.Status);

        await _repository.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == email),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Lança identity.user.email.duplicate quando o e-mail já está em uso")]
    public async Task HandleAsync_Throws_WhenEmailIsAlreadyTaken()
    {
        // Arrange
        var email = Email.Create("ocupado@example.com");
        RepositoryFinds(User.Create("Já existe", email));

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            new CreateUserCommandHandler(_repository)
                .HandleAsync(new CreateUserCommand("Luiz Palma", email)));

        // Assert
        Assert.Equal("identity.user.email.duplicate", exception.Code);
    }

    [Fact(DisplayName = "E-mail duplicado nem chega a tentar escrever")]
    public async Task HandleAsync_DoesNotWrite_WhenEmailIsAlreadyTaken()
    {
        // Arrange
        var email = Email.Create("ocupado@example.com");
        RepositoryFinds(User.Create("Já existe", email));

        // Act
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            new CreateUserCommandHandler(_repository)
                .HandleAsync(new CreateUserCommand("Luiz Palma", email)));

        // Assert
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    private void RepositoryFinds(User? user) =>
        _repository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns(user);
}
