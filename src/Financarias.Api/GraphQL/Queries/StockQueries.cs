using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Application.MarketData.Stocks.UseCases;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class StockQueries
{
    [GraphQLName("stockQuote")]
    public Task<StockQuoteResult?> GetStockQuoteAsync(
        string ticker,
        IGetStockQuoteUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(ticker, cancellationToken);
}
