using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.DTOs.Results;

namespace Financarias.Application.Identity.Users.UseCases;

public interface IChangePasswordUseCase
{
    /// <summary>
    ///     Troca a senha do usuário corrente. A nova senha precisa cumprir a política de composição, e a atual
    ///     precisa conferir; qualquer um dos dois erros lança antes de qualquer escrita. Exige usuário autenticado.
    /// </summary>
    Task<UserResult> ExecuteAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
