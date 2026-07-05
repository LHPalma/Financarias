using Financarias.Application.MarketData.Stocks.DTOs.Results;

namespace Financarias.Application.MarketData.Stocks.UseCases;

/// <summary>
///     Caso de uso de consulta de cotação por ticker: valida a entrada crua e orquestra a busca.
/// </summary>
public interface IGetStockQuoteUseCase
{
    Task<StockQuoteResult?> ExecuteAsync(string ticker, CancellationToken cancellationToken = default);
}