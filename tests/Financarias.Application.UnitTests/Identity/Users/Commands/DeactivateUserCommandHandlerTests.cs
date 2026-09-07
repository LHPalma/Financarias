using Financarias.Application.Common.Persistence;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using NSubstitute;

namespace Financarias.Application.UnitTests.Identity.Users.Commands;

public class DeactivateUserCommandHandlerTests
{
    private readonly IRepository<User> _repository = Substitute.For<IRepository<User>>();

    [Fact(DisplayName = "Desativa o usuário e persiste")]
    public async Task HandleAsync_DeactivatesUser()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"));
        RepositoryFinds(user);

        // Act
        var result = await new DeactivateUserCommandHandler(_repository)
            .HandleAsync(new DeactivateUserCommand(user.Id));

        // Assert
        Assert.Equal(UserStatus.Inactive, result.Status);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Desativar quem já está inativo não é erro")]
    public async Task HandleAsync_IsNoOp_WhenAlreadyInactive()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"));
        user.Deactivate();
        RepositoryFinds(user);

        // Act
        var result = await new DeactivateUserCommandHandler(_repository)
            .HandleAsync(new DeactivateUserCommand(user.Id));

        // Assert
        Assert.Equal(UserStatus.Inactive, result.Status);
    }

    [Fact(DisplayName = "Lança identity.user.notfound quando o usuário não existe")]
    public async Task HandleAsync_Throws_WhenUserDoesNotExist()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        RepositoryFinds(null);

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            new DeactivateUserCommandHandler(_repository).HandleAsync(new DeactivateUserCommand(id)));

        // Assert
        Assert.Equal("identity.user.notfound", exception.Code);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void RepositoryFinds(User? user) =>
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);
}
