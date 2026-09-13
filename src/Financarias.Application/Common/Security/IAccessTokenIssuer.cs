namespace Financarias.Application.Common.Security;

/// <summary>
///     Emite o token que prova a identidade de um usuário. Quem chama não sabe o formato, o
///     algoritmo nem a chave — trocar JWT por outro mecanismo não toca caso de uso nenhum.
/// </summary>
public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(Guid userId);
}