using Financarias.Application.MarketData.Indexes.DTOs.Requests;
using Financarias.Application.MarketData.Indexes.Import;

namespace Financarias.Application.MarketData.Indexes.UseCases;

public interface IImportIndexQuotesUseCase
{
    /// <summary>
    /// Importa da B3 os fechamentos diários de um índice em um ano e persiste por upsert; lança quando o
    /// ano é anterior ao primeiro ano publicado.
    /// </summary>
    Task<IndexQuoteImportResult> ExecuteAsync(
        ImportIndexQuotesRequest request,
        CancellationToken cancellationToken = default);
}
