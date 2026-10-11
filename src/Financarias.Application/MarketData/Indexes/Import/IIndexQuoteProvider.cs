using Financarias.Domain.MarketData.Indexes;

namespace Financarias.Application.MarketData.Indexes.Import;

/// <summary>
/// Porta de origem dos fechamentos diários de um índice para importação: obtém, de um provedor externo
/// (ex.: estatísticas por índice da B3), todos os pregões de um ano. Ano sem dados (por exemplo, futuro)
/// devolve lista vazia; falha de transporte propaga.
/// </summary>
public interface IIndexQuoteProvider
{
    Task<IReadOnlyList<IndexQuoteImportItem>> GetYearAsync(
        MarketIndex marketIndex,
        int year,
        CancellationToken cancellationToken = default);
}
