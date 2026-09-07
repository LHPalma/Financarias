using Financarias.Application.Common.Messaging;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Application.Identity.Users.DTOs.Results;
using Financarias.Application.Identity.Users.Mappers;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.UseCases;

public class DeactivateUserUseCase(
    ICommandHandler<DeactivateUserCommand, User> handler,
    UserMapper mapper
) : IDeactivateUserUseCase
{
    public async Task<UserResult> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await handler.HandleAsync(new DeactivateUserCommand(id), cancellationToken);

        return mapper.ToResult(user);
    }
}
