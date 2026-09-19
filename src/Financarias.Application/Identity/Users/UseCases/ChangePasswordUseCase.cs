using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.DTOs.Results;
using Financarias.Application.Identity.Users.Mappers;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.UseCases;

public class ChangePasswordUseCase(
    ICommandHandler<ChangePasswordCommand, User> handler,
    ICurrentUser currentUser,
    UserMapper mapper
) : IChangePasswordUseCase
{
    public async Task<UserResult> ExecuteAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
                     ?? throw new InvalidOperationException("Changing a password requires an authenticated user.");
        var newPassword = Password.Create(request.NewPassword);

        var user = await handler.HandleAsync(
            new ChangePasswordCommand(userId, request.CurrentPassword, newPassword),
            cancellationToken);

        return mapper.ToResult(user);
    }
}
