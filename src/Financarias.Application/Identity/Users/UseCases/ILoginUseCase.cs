using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.DTOs.Results;

namespace Financarias.Application.Identity.Users.UseCases;

public interface ILoginUseCase
{
    /// <summary>
    ///     Troca e-mail e senha por um token de acesso. E-mail malformado, e-mail sem conta, senha errada e
    ///     usuário inativo lançam o mesmo erro, para o login não revelar quais contas existem.
    /// </summary>
    Task<AccessTokenResult> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
