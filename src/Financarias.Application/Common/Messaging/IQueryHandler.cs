namespace Financarias.Application.Common.Messaging;

/// <summary>
/// Executa uma <see cref="IQuery{TResult}"/> e devolve o resultado correspondente.
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}