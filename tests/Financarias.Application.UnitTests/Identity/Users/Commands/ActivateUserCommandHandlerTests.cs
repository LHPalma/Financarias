using Financarias.Application.Common.Persistence;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using NSubstitute;

namespace Financarias.Application.UnitTests.Identity.Users.Commands;

public class ActivateUserCommandHandlerTests
{
    private readonly IRepository<User> _repository = Substitute.For<IRepository<User>>();

    [Fact(DisplayName = "Reativa o usuário inativo e persiste")]
    public async Task HandleAsync_ActivatesUser()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"));
        user.Deactivate();
        RepositoryFinds(user);

        // Act
        var result = await new ActivateUserCommandHandler(_repository)
            .HandleAsync(new ActivateUserCommand(user.Id));

        // Assert
        Assert.Equal(UserStatus.Active, result.Status);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Reativar quem já está ativo não é erro")]
    public async Task HandleAsync_IsNoOp_WhenAlreadyActive()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"));
        RepositoryFinds(user);

        // Act
        var result = await new ActivateUserCommandHandler(_repository)
            .HandleAsync(new ActivateUserCommand(user.Id));

        // Assert
        Assert.Equal(UserStatus.Active, result.Status);
    }

    [Fact(DisplayName = "Lança identity.user.notfound quando o usuário não existe")]
    public async Task HandleAsync_Throws_WhenUserDoesNotExist()
    {
        // Arrange
        RepositoryFinds(null);

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            new ActivateUserCommandHandler(_repository)
                .HandleAsync(new ActivateUserCommand(Guid.CreateVersion7())));

        // Assert
        Assert.Equal("identity.user.notfound", exception.Code);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void RepositoryFinds(User? user) =>
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);
}
