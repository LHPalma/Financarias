using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.DTOs.Results;
using Financarias.Application.Identity.Users.UseCases;
using HotChocolate.Authorization;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class UserMutations
{
    /// <summary>
    ///     Cria um usuário ativo, com senha. Não exige autenticação.
    ///
    ///     Erros (`extensions.code`):
    ///     - `contacts.email.invalid`: e-mail malformado.
    ///     - `identity.user.email.duplicate`: e-mail já em uso.
    ///     - `identity.user.name.required`: nome em branco.
    ///     - `identity.password.tooshort`, `identity.password.toolong`, `identity.password.missinglowercase`, `identity.password.missinguppercase`, `identity.password.missingdigit`, `identity.password.missingspecial`: senha fora da política.
    /// </summary>
    /// <param name="input">Nome, e-mail e senha do novo usuário.</param>
    [GraphQLName("createUser")]
    public Task<UserResult> CreateUserAsync(
        CreateUserRequest input,
        ICreateUserUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);

    /// <summary>
    ///     Troca e-mail e senha por um token de acesso, que vai no header `Authorization` como Bearer. Não exige autenticação.
    ///
    ///     Erro (`extensions.code`): `identity.credentials.invalid`. É o mesmo para e-mail malformado, e-mail sem conta, senha errada e usuário desativado, de propósito, para o login não revelar quais contas existem.
    /// </summary>
    /// <param name="input">E-mail e senha.</param>
    [GraphQLName("login")]
    public Task<AccessTokenResult> LoginAsync(
        LoginRequest input,
        ILoginUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);

    /// <summary>
    ///     Troca a senha do usuário dono do token. Não recebe id, então não há como trocar a senha de outra conta. Exige autenticação: sem token válido devolve o erro `AUTH_NOT_AUTHENTICATED`.
    ///
    ///     Erros (`extensions.code`):
    ///     - `identity.password.currentincorrect`: senha atual errada.
    ///     - `identity.password.tooshort`, `identity.password.toolong`, `identity.password.missinglowercase`, `identity.password.missinguppercase`, `identity.password.missingdigit`, `identity.password.missingspecial`: nova senha fora da política.
    ///
    ///     Não invalida os tokens já emitidos.
    /// </summary>
    /// <param name="input">Senha atual e nova senha.</param>
    [GraphQLName("changePassword")]
    [Authorize]
    public Task<UserResult> ChangePasswordAsync(
        ChangePasswordRequest input,
        IChangePasswordUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);

    /// <summary>
    ///     Reativa o usuário. Repetir em quem já está ativo não tem efeito. Exige autenticação: sem token válido devolve o erro `AUTH_NOT_AUTHENTICATED`.
    ///
    ///     Erro (`extensions.code`): `identity.user.notfound`.
    ///
    ///     Hoje qualquer usuário autenticado pode reativar qualquer conta.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    [GraphQLName("activateUser")]
    [Authorize]
    public Task<UserResult> ActivateUserAsync(
        Guid id,
        IActivateUserUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(id, cancellationToken);

    /// <summary>
    ///     Desativa o usuário, que deixa de conseguir fazer login. Repetir em quem já está inativo não tem efeito. Exige autenticação: sem token válido devolve o erro `AUTH_NOT_AUTHENTICATED`.
    ///
    ///     Erro (`extensions.code`): `identity.user.notfound`.
    ///
    ///     Um token já emitido continua valendo até expirar. Hoje qualquer usuário autenticado pode desativar qualquer conta.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    [GraphQLName("deactivateUser")]
    [Authorize]
    public Task<UserResult> DeactivateUserAsync(
        Guid id,
        IDeactivateUserUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(id, cancellationToken);
}
