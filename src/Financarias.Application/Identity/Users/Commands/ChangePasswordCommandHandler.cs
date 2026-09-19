using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.Common.Security;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Commands;

public sealed class ChangePasswordCommandHandler(
    IRepository<User> repository,
    IPasswordHasher passwordHasher
) : ICommandHandler<ChangePasswordCommand, User>
{
    public async Task<User> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await repository.GetByIdAsync(command.UserId, cancellationToken)
                   ?? throw IdentityErrors.NotFound(command.UserId);

        if (!passwordHasher.Verify(user.PasswordHash, command.CurrentPassword))
        {
            throw IdentityErrors.IncorrectCurrentPassword();
        }

        user.ChangePassword(passwordHasher.Hash(command.NewPassword));

        await repository.SaveChangesAsync(cancellationToken);

        return user;
    }
}