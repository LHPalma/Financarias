using Financarias.Application.Common.Messaging;
using Financarias.Application.Identity.Users.Commands;
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
    public async Task<UserResult> ExecuteAsync(string name, string email, CancellationToken cancellationToken = default)
    {
        var user = await handler.HandleAsync(
            new CreateUserCommand(name, Email.Create(email)),
            cancellationToken);

        return mapper.ToResult(user);
    }
}