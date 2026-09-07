using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.Identity.Users.Specifications;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.Commands;

public class CreateUserCommandHandler(
    IRepository<User> repository
) : ICommandHandler<CreateUserCommand, User>
{
    public async Task<User> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.FirstOrDefaultAsync(
            new UserByEmailSpecification(command.Email),
            cancellationToken);

        if (existing is not null)
        {
            throw IdentityErrors.DuplicateEmail(command.Email.Value);
        }

        var user = User.Create(command.Name, command.Email);

        await repository.AddAsync(user, cancellationToken);

        return user;
    }
}