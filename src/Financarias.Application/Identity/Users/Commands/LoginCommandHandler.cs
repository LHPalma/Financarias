using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Specifications;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Commands;

public class LoginCommandHandler(
    IRepository<User> repository,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer
) : ICommandHandler<LoginCommand, IssuedAccessToken>
{
    public async Task<IssuedAccessToken> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await repository.FirstOrDefaultAsync(
            new UserByEmailSpecification(command.Email),
            cancellationToken);

        var passwordMatches = passwordHasher.Verify(user?.PasswordHash, command.Password);

        if (user is null || !passwordMatches || user.Status != UserStatus.Active)
        {
            throw IdentityErrors.InvalidCredentials();
        }

        return tokenIssuer.Issue(user.Id);
    }
}