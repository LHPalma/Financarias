using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Application.MarketData.Stocks.Gateways;

namespace Financarias.Application.MarketData.Stocks.Queries;

public class GetStockQuoteQueryHandler(
    IStockQuoteGateway gateway
) : IQueryHandler<GetStockQuoteQuery, StockQuoteResult?>
{
    public Task<StockQuoteResult?> HandleAsync(GetStockQuoteQuery query, CancellationToken cancellationToken = default)
        => gateway.FindQuoteAsync(query.Ticker, cancellationToken);
}