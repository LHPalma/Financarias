namespace Financarias.Application.Common.Security;

/// <summary>
///     Informa quem é o usuário corrente da requisição, sem dizer como essa identidade foi determinada.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Id do usuário corrente, ou <c>null</c> quando não há requisição ou identidade declarada.</summary>
    Guid? UserId { get; }
}