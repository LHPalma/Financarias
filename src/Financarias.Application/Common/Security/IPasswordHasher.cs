using Financarias.Domain.Identity;

namespace Financarias.Application.Common.Security;

/// <summary>
///     Deriva e verifica senhas. A implementação escolhe o algoritmo e o custo; quem chama não
///     sabe quais são, e por isso subir o custo ou trocar o algoritmo não toca caso de uso nenhum.
/// </summary>
public interface IPasswordHasher
{
    PasswordHash Hash(Password password);

    bool Verify(PasswordHash hash, string password);
}