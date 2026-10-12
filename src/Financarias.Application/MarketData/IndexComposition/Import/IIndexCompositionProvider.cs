namespace Financarias.Application.MarketData.IndexComposition.Import;

/// <summary>
/// Porta de origem das carteiras teóricas históricas dos índices: lê, de um arquivo publicado pelo provedor
/// (ex.: planilha ou ZIP anexado a um comunicado da B3), todas as carteiras que ele traz, uma por índice.
/// Arquivo que não contém carteira, como o de entradas e saídas, devolve lista vazia; falha de transporte
/// ou arquivo ilegível propaga.
/// </summary>
public interface IIndexCompositionProvider
{
    Task<IReadOnlyList<IndexPortfolioImportItem>> GetAsync(string sourceUrl, CancellationToken cancellationToken = default);
}
