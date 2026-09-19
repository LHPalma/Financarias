using Financarias.Application.Common.Persistence;
using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using NSubstitute;

namespace Financarias.Application.UnitTests.Identity.Users.Commands;

public class ChangePasswordCommandHandlerTests
{
    private const string CurrentSecret = "Atual-Senha1!";
    private const string NewSecret = "Nova-Senha2@";

    private static readonly PasswordHash NewHash = PasswordHash.Create(
        "$argon2id$v=19$m=19456,t=2,p=1$b3V0cm9zYWx0$b3V0cm9oYXNo",
        pepperVersion: 2);

    private readonly IRepository<User> _repository = Substitute.For<IRepository<User>>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();

    [Fact(DisplayName = "Troca o hash do usuário e persiste quando a senha atual confere")]
    public async Task HandleAsync_ChangesTheHash_WhenCurrentPasswordMatches()
    {
        // Arrange
        var user = CreateUser();
        RepositoryFinds(user);
        HasherAccepts(true);
        _hasher.Hash(Arg.Any<Password>()).Returns(NewHash);

        // Act
        var result = await CreateHandler().HandleAsync(
            new ChangePasswordCommand(user.Id, CurrentSecret, Password.Create(NewSecret)));

        // Assert
        Assert.Same(user, result);
        Assert.Equal(NewHash, user.PasswordHash);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Hasheia a nova senha exatamente como veio")]
    public async Task HandleAsync_HashesTheNewPassword()
    {
        // Arrange
        var user = CreateUser();
        RepositoryFinds(user);
        HasherAccepts(true);
        _hasher.Hash(Arg.Any<Password>()).Returns(NewHash);

        // Act
        await CreateHandler().HandleAsync(
            new ChangePasswordCommand(user.Id, CurrentSecret, Password.Create(NewSecret)));

        // Assert
        _hasher.Received(1).Hash(Arg.Is<Password>(p => p.Value == NewSecret));
    }

    [Fact(DisplayName = "Confere a senha atual contra o hash gravado, antes de hashear a nova")]
    public async Task HandleAsync_VerifiesTheCurrentPassword_BeforeHashingTheNewOne()
    {
        // Arrange
        var user = CreateUser();
        RepositoryFinds(user);
        HasherAccepts(true);
        _hasher.Hash(Arg.Any<Password>()).Returns(NewHash);

        // Act
        await CreateHandler().HandleAsync(
            new ChangePasswordCommand(user.Id, CurrentSecret, Password.Create(NewSecret)));

        // Assert: senha atual errada gasta um Argon2, e não dois
        Received.InOrder(() =>
        {
            _hasher.Verify(TestPasswordHashes.Any, CurrentSecret);
            _hasher.Hash(Arg.Any<Password>());
        });
    }

    [Fact(DisplayName = "Senha atual errada lança identity.password.currentincorrect e não escreve nada")]
    public async Task HandleAsync_Throws_WhenCurrentPasswordIsWrong()
    {
        // Arrange
        var user = CreateUser();
        RepositoryFinds(user);
        HasherAccepts(false);

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateHandler().HandleAsync(
                new ChangePasswordCommand(user.Id, "Errada-Senha1!", Password.Create(NewSecret))));

        // Assert
        Assert.Equal("identity.password.currentincorrect", exception.Code);
        Assert.Equal(TestPasswordHashes.Any, user.PasswordHash);
        _hasher.DidNotReceive().Hash(Arg.Any<Password>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Lança identity.user.notfound quando o usuário não existe, sem chegar ao hasher")]
    public async Task HandleAsync_Throws_WhenUserDoesNotExist()
    {
        // Arrange
        RepositoryFinds(null);

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateHandler().HandleAsync(
                new ChangePasswordCommand(Guid.CreateVersion7(), CurrentSecret, Password.Create(NewSecret))));

        // Assert
        Assert.Equal("identity.user.notfound", exception.Code);
        _hasher.DidNotReceive().Verify(Arg.Any<PasswordHash?>(), Arg.Any<string>());
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private ChangePasswordCommandHandler CreateHandler() => new(_repository, _hasher);

    private static User CreateUser() =>
        User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any);

    private void HasherAccepts(bool accepts) =>
        _hasher.Verify(Arg.Any<PasswordHash?>(), Arg.Any<string>()).Returns(accepts);

    private void RepositoryFinds(User? user) =>
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(user);
}
