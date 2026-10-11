using Financarias.Application.MarketData.Stocks.DTOs.Requests;
using Financarias.Application.MarketData.Stocks.Import;

namespace Financarias.Application.MarketData.Stocks.UseCases;

public interface IImportDailyQuotesUseCase
{
    /// <summary>
    /// Importa as cotações do COTAHIST da B3 do período pedido e persiste por upsert; lança quando o
    /// pedido não é exatamente um dia, um mês ou um ano válidos.
    /// </summary>
    Task<DailyQuoteImportResult> ExecuteAsync(
        ImportDailyQuotesRequest request,
        CancellationToken cancellationToken = default);
}
