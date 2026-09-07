using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Commands;

public class ActivateUserCommandHandler(
    IRepository<User> repository
) : ICommandHandler<ActivateUserCommand, User>
{
    public async Task<User> HandleAsync(
        ActivateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(command.UserId, cancellationToken)
                   ?? throw IdentityErrors.NotFound(command.UserId);

        user.Activate();

        await repository.SaveChangesAsync(cancellationToken);

        return user;
    }
}
