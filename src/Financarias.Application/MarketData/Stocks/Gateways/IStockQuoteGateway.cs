using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.Stocks.Gateways;

/// <summary>
///     Porta de saída para consulta de cotação de um ativo por ticker. Retorna <c>null</c>
///     quando a cotação não é encontrada; <b>propaga exceção</b> quando a fonte externa falha.
/// </summary>
public interface IStockQuoteGateway
{
    Task<StockQuoteResult?> FindQuoteAsync(Ticker ticker, CancellationToken cancellationToken = default);
}