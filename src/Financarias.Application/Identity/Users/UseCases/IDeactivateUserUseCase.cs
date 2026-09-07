using Financarias.Application.Identity.Users.DTOs.Results;

namespace Financarias.Application.Identity.Users.UseCases;

public interface IDeactivateUserUseCase
{
    /// <summary>Desativa o usuário; repetir em quem já está inativo não tem efeito. Lança se não existir.</summary>
    Task<UserResult> ExecuteAsync(Guid id, CancellationToken cancellationToken = default);
}
