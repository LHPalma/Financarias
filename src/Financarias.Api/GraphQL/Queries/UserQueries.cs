using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Queries;
using Financarias.Domain.Identity;
using HotChocolate.Authorization;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class UserQueries
{
    /// <summary>
    ///     Lista os usuários, com filtro (`where`) e ordenação (`order`). Por padrão devolve só os ativos. Exige autenticação: sem token válido devolve o erro `AUTH_NOT_AUTHENTICATED`.
    ///
    ///     Hoje qualquer usuário autenticado enxerga a lista inteira.
    /// </summary>
    /// <param name="includeInactive">Inclui também os usuários desativados. Padrão: falso.</param>
    [GraphQLName("users")]
    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers(IUserReads reads, bool includeInactive = false) =>
        includeInactive
            ? reads.Users()
            : reads.Users().Where(user => user.Status == UserStatus.Active);

    /// <summary>
    ///     Busca um usuário pelo id. Devolve nulo se não existir. Exige autenticação: sem token válido devolve o erro `AUTH_NOT_AUTHENTICATED`.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    [GraphQLName("user")]
    [Authorize]
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<User> GetUserById(Guid id, IUserReads reads) =>
        reads.Users().Where(user => user.Id == id);

    /// <summary>
    ///     Devolve o usuário dono do token enviado. Exige autenticação: sem token válido devolve o erro `AUTH_NOT_AUTHENTICATED`.
    /// </summary>
    [GraphQLName("me")]
    [Authorize]
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<User> GetCurrentUser(IUserReads reads, ICurrentUser currentUser) =>
        reads.Users().Where(user => user.Id == currentUser.UserId);
}
