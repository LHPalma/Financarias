using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Commands;

public class DeactivateUserCommandHandler(
    IRepository<User> repository
) : ICommandHandler<DeactivateUserCommand, User>
{
    public async Task<User> HandleAsync(
        DeactivateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(command.UserId, cancellationToken)
                   ?? throw IdentityErrors.NotFound(command.UserId);

        user.Deactivate();

        await repository.SaveChangesAsync(cancellationToken);

        return user;
    }
}
