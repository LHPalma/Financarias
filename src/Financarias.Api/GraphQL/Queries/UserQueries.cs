using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Queries;
using Financarias.Domain.Identity;
using HotChocolate.Authorization;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class UserQueries
{
    [GraphQLName("users")]
    [Authorize]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> GetUsers(IUserReads reads, bool includeInactive = false) =>
        includeInactive
            ? reads.Users()
            : reads.Users().Where(user => user.Status == UserStatus.Active);

    [GraphQLName("user")]
    [Authorize]
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<User> GetUserById(Guid id, IUserReads reads) =>
        reads.Users().Where(user => user.Id == id);

    [GraphQLName("me")]
    [Authorize]
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<User> GetCurrentUser(IUserReads reads, ICurrentUser currentUser) =>
        reads.Users().Where(user => user.Id == currentUser.UserId);
}
