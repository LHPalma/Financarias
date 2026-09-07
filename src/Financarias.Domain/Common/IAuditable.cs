namespace Financarias.Domain.Common;

/// <summary>
///     Marca uma entidade que, além do tempo, registra a autoria da escrita — nula quando
///     não houve requisição (import, migration, job).
/// </summary>
public interface IAuditable : IHasTimestamps
{
    Guid? CreatedBy { get; }

    Guid? UpdatedBy { get; }
}