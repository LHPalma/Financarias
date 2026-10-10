namespace Financarias.Application.MarketData.Stocks.Import;

/// <summary>
/// Arquivo de cotações já baixado e aberto para leitura. Segura o recurso local (arquivo temporário)
/// até ser descartado, então quem o recebe deve usá-lo com <c>await using</c>.
/// </summary>
public interface IDailyQuoteFile : IAsyncDisposable
{
    /// <summary>
    /// Quantas linhas de escopo (mercado à vista e fracionário) foram puladas por estarem malformadas.
    /// Só é definitivo depois que <see cref="ReadAsync"/> foi enumerado até o fim.
    /// </summary>
    int RowsSkipped { get; }

    /// <summary>
    /// Transmite (streaming) as cotações do arquivo, uma a uma, sem materializar o arquivo inteiro.
    /// Linhas fora de escopo (opções, termo etc.) são ignoradas sem contar como puladas.
    /// </summary>
    IAsyncEnumerable<DailyQuoteImportItem> ReadAsync(CancellationToken cancellationToken = default);
}
