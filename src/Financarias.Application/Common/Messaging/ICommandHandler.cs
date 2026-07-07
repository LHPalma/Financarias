namespace Financarias.Application.Common.Messaging;

/// <summary>
/// Executa um <see cref="ICommand{TResult}"/> e devolve o resultado correspondente.
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
