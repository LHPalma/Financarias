namespace Financarias.Domain.Common;

/// <summary>
///     Marca uma entidade cujos carimbos de tempo são preenchidos pela infraestrutura,
///     nunca pela própria entidade.
/// </summary>
public interface IHasTimestamps
{
    DateTimeOffset CreatedAt { get; }

    DateTimeOffset UpdatedAt { get; }
}