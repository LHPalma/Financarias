using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Application.MarketData.Stocks.UseCases;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class StockQueries
{
    /// <summary>
    ///     Cotação de uma ação pelo ticker (fonte: Brapi). Devolve nulo se o ticker não existir ou se a fonte não tiver preço. Não exige autenticação.
    ///
    ///     Erro (`extensions.code`): `stock.ticker.invalid`, quando o ticker não tem uma letra, três letras ou dígitos, 1 ou 2 dígitos e, opcionalmente, um F final (fracionário).
    /// </summary>
    /// <param name="ticker">Ticker do papel, como PETR4, PETR4F, B3SA3 ou AAPL34 (BDR). Maiúsculas e minúsculas são aceitas, e espaços nas pontas são ignorados.</param>
    [GraphQLName("stockQuote")]
    public Task<StockQuoteResult?> GetStockQuoteAsync(
        string ticker,
        IGetStockQuoteUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(ticker, cancellationToken);
}
