using Financarias.Application.Common.Messaging;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.DTOs.Results;
using Financarias.Application.Identity.Users.Mappers;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.UseCases;

public class CreateUserUseCase(
    ICommandHandler<CreateUserCommand, User> handler,
    UserMapper mapper
) : ICreateUserUseCase
{
    public async Task<UserResult> ExecuteAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await handler.HandleAsync(
            new CreateUserCommand(request.Name, Email.Create(request.Email)),
            cancellationToken);

        return mapper.ToResult(user);
    }
}
