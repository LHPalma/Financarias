using Financarias.Application.Common.Persistence;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Queries;

public class UserReads(IApplicationDbContext db) : IUserReads
{
    public IQueryable<User> Users() => db.Users;
}
