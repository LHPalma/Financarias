namespace Financarias.Application.MarketData.SearchInterest.Import;

/// <summary>
/// Porta de origem do interesse de busca: obtém, de um provedor externo (ex.: Google Trends), o índice diário
/// de 0 a 100 de cada termo de um grupo comparado, em um país e um intervalo. O índice é relativo ao grupo e ao
/// intervalo pedidos. Termo sem busca devolve zeros; falha de transporte propaga.
/// </summary>
public interface ISearchInterestProvider
{
    Task<IReadOnlyList<SearchInterestImportItem>> GetAsync(
        IReadOnlyList<string> terms,
        string geo,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
