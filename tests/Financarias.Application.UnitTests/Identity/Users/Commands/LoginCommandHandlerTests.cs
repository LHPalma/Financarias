using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;
using NSubstitute;

namespace Financarias.Application.UnitTests.Identity.Users.Commands;

public class LoginCommandHandlerTests
{
    private const string Secret = "S3nha-Forte!";

    private static readonly IssuedAccessToken Issued =
        new("token-emitido", new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));

    private readonly IRepository<User> _repository = Substitute.For<IRepository<User>>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IAccessTokenIssuer _issuer = Substitute.For<IAccessTokenIssuer>();

    [Fact(DisplayName = "Credencial certa emite o token com o id do usuário")]
    public async Task HandleAsync_IssuesToken_ForValidCredentials()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any);
        RepositoryFinds(user);
        HasherAccepts(true);
        _issuer.Issue(user.Id).Returns(Issued);

        // Act
        var token = await CreateHandler().HandleAsync(new LoginCommand(user.Email, Secret));

        // Assert
        Assert.Equal(Issued, token);
        _hasher.Received(1).Verify(user.PasswordHash, Secret);
    }

    [Fact(DisplayName = "Procura o usuário pelo e-mail do comando")]
    public async Task HandleAsync_QueriesTheRepository_ByEmail()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any);
        var other = User.Create("Outro", Email.Create("outro@example.com"), TestPasswordHashes.Any);
        RepositoryFinds(user);
        HasherAccepts(true);
        _issuer.Issue(user.Id).Returns(Issued);

        // Act
        await CreateHandler().HandleAsync(new LoginCommand(user.Email, Secret));

        // Assert: a specification recebida, avaliada sobre dois usuários, devolve só o do e-mail
        await _repository.Received(1).FirstOrDefaultAsync(
            Arg.Is<ISpecification<User>>(spec => spec.Evaluate(new[] { user, other }).SequenceEqual(new[] { user })),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "E-mail inexistente lança identity.credentials.invalid")]
    public async Task HandleAsync_Throws_WhenUserDoesNotExist()
    {
        // Arrange
        RepositoryFinds(null);

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateHandler().HandleAsync(new LoginCommand(Email.Create("ninguem@example.com"), Secret)));

        // Assert
        Assert.Equal("identity.credentials.invalid", exception.Code);
        _issuer.DidNotReceive().Issue(Arg.Any<Guid>());
    }

    [Fact(DisplayName = "Senha errada lança identity.credentials.invalid")]
    public async Task HandleAsync_Throws_WhenPasswordIsWrong()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any);
        RepositoryFinds(user);
        HasherAccepts(false);

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateHandler().HandleAsync(new LoginCommand(user.Email, "Outra-Senha1!")));

        // Assert
        Assert.Equal("identity.credentials.invalid", exception.Code);
        _issuer.DidNotReceive().Issue(Arg.Any<Guid>());
    }

    [Fact(DisplayName = "Usuário inativo com a senha certa lança identity.credentials.invalid")]
    public async Task HandleAsync_Throws_WhenUserIsInactive()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any);
        user.Deactivate();
        RepositoryFinds(user);
        HasherAccepts(true);

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateHandler().HandleAsync(new LoginCommand(user.Email, Secret)));

        // Assert
        Assert.Equal("identity.credentials.invalid", exception.Code);
        _issuer.DidNotReceive().Issue(Arg.Any<Guid>());
    }

    [Fact(DisplayName = "Os três casos de falha devolvem o mesmo código e a mesma mensagem")]
    public async Task HandleAsync_FailureCases_AreIndistinguishable()
    {
        // Arrange
        var active = User.Create("Ativo", Email.Create("ativo@example.com"), TestPasswordHashes.Any);
        var inactive = User.Create("Inativo", Email.Create("inativo@example.com"), TestPasswordHashes.Any);
        inactive.Deactivate();

        // Act
        RepositoryFinds(null);
        HasherAccepts(false);
        var unknown = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateHandler().HandleAsync(new LoginCommand(active.Email, Secret)));

        RepositoryFinds(active);
        var wrongPassword = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateHandler().HandleAsync(new LoginCommand(active.Email, Secret)));

        RepositoryFinds(inactive);
        HasherAccepts(true);
        var inactiveUser = await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateHandler().HandleAsync(new LoginCommand(inactive.Email, Secret)));

        // Assert: distinguir qualquer um dos três transforma o login num oráculo de cadastro
        Assert.Equal(unknown.Code, wrongPassword.Code);
        Assert.Equal(unknown.Code, inactiveUser.Code);
        Assert.Equal(unknown.Message, wrongPassword.Message);
        Assert.Equal(unknown.Message, inactiveUser.Message);
    }

    [Fact(DisplayName = "E-mail inexistente passa pelo Verify com hash nulo")]
    public async Task HandleAsync_VerifiesAgainstNullHash_WhenUserDoesNotExist()
    {
        // Arrange
        RepositoryFinds(null);

        // Act
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateHandler().HandleAsync(new LoginCommand(Email.Create("ninguem@example.com"), Secret)));

        // Assert: afirma o caminho, não o tempo — cronômetro oscila no CI. É o Verify recebendo
        // nulo que gasta o custo do Argon2 e iguala o tempo de resposta ao de um usuário real.
        _hasher.Received(1).Verify(null, Secret);
    }

    [Fact(DisplayName = "Usuário inativo tem a senha verificada antes de qualquer decisão")]
    public async Task HandleAsync_VerifiesThePassword_BeforeLookingAtStatus()
    {
        // Arrange
        var user = User.Create("Luiz Palma", Email.Create("luiz@example.com"), TestPasswordHashes.Any);
        user.Deactivate();
        RepositoryFinds(user);
        HasherAccepts(false);

        // Act
        await Assert.ThrowsAsync<DomainValidationException>(() =>
            CreateHandler().HandleAsync(new LoginCommand(user.Email, Secret)));

        // Assert: checar Inactive antes revelaria a conta desativada sem saber a senha (RN-18)
        _hasher.Received(1).Verify(user.PasswordHash, Secret);
    }

    private LoginCommandHandler CreateHandler() => new(_repository, _hasher, _issuer);

    private void HasherAccepts(bool accepts) =>
        _hasher.Verify(Arg.Any<PasswordHash?>(), Arg.Any<string>()).Returns(accepts);

    private void RepositoryFinds(User? user) =>
        _repository
            .FirstOrDefaultAsync(Arg.Any<ISpecification<User>>(), Arg.Any<CancellationToken>())
            .Returns(user);
}
