using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.DTOs.Results;

namespace Financarias.Application.Identity.Users.UseCases;

public interface ICreateUserUseCase
{
    /// <summary>Cria um usuário ativo; lança quando o e-mail é inválido ou já está em uso.</summary>
    Task<UserResult> ExecuteAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
}
