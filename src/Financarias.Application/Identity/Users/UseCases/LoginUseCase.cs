using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.DTOs.Results;
using Financarias.Domain.Contacts;
using Financarias.Domain.Identity;

namespace Financarias.Application.Identity.Users.UseCases;

public class LoginUseCase(
    ICommandHandler<LoginCommand, IssuedAccessToken> handler
) : ILoginUseCase
{
    public async Task<AccessTokenResult> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Email.TryCreate(request.Email, out var email))
        {
            throw IdentityErrors.InvalidCredentials();
        }

        var token = await handler.HandleAsync(new LoginCommand(email, request.Password), cancellationToken);

        return new AccessTokenResult(token.Token, token.ExpiresAt);
    }
}
