using Financarias.Application.Identity.Users.DTOs.Results;

namespace Financarias.Application.Identity.Users.UseCases;

public interface IActivateUserUseCase
{
    /// <summary>Reativa o usuário; repetir em quem já está ativo não tem efeito. Lança se não existir.</summary>
    Task<UserResult> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}
