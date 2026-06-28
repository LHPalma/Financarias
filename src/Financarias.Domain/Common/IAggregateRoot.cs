namespace Financarias.Domain.Common;

/// <summary>
/// Marca uma entidade como raiz de agregado — único tipo que pode ter um repositório de escrita
/// (<c>IRepository&lt; T&gt;</c> só aceita <c>T : IAggregateRoot</c>).
/// </summary>
public interface IAggregateRoot
{
}