using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Application.MarketData.Stocks.Queries;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.Stocks.UseCases;

public sealed class GetStockQuoteUseCase(
    IQueryHandler<GetStockQuoteQuery, StockQuoteResult?> handler
) : IGetStockQuoteUseCase
{
    public async Task<StockQuoteResult?> ExecuteAsync(string ticker, CancellationToken cancellationToken = default)
    {
        var validTicker = Ticker.Create(ticker);

        return await handler.HandleAsync(new GetStockQuoteQuery(validTicker), cancellationToken);
    }
}