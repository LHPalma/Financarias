using Financarias.Application.Identity.Users.DTOs.Results;

namespace Financarias.Application.Identity.Users.UseCases;

public interface ICreateUserUseCase
{
    Task<UserResult> ExecuteAsync(
        string name,
        string email,
        CancellationToken cancellationToken = default
    );
}