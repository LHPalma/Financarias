using Financarias.Domain.Identity;

namespace Financarias.Application.Common.Security;

/// <summary>
///     Deriva e verifica senhas. A implementação escolhe o algoritmo e o custo; quem chama não
///     sabe quais são, e por isso subir o custo ou trocar o algoritmo não toca caso de uso nenhum.
/// </summary>
public interface IPasswordHasher
{
    PasswordHash Hash(Password password);

    /// <summary>
    ///     Confere a senha contra o hash. Com hash nulo — usuário inexistente —, verifica contra um hash
    ///     descartável e devolve false, gastando o mesmo tempo: a ausência do usuário não fica visível
    ///     pelo tempo de resposta.
    /// </summary>
    bool Verify(PasswordHash? hash, string password);
}