using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.UseCases;
using Financarias.Domain.MarketData;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class ForeignExchangeQueries
{
    [GraphQLName("convertCurrency")]
    public Task<ConversionResult?> ConvertCurrencyAsync(
        IConvertCurrencyUseCase useCase,
        CancellationToken cancellationToken,
        decimal amount,
        Currency from,
        Currency to,
        int decimals = 2) =>
        useCase.ExecuteAsync(amount, from, to, decimals, cancellationToken);

    [GraphQLName("currencyPrices")]
    public Task<IReadOnlyList<CurrencyPriceResult>> GetCurrencyPricesAsync(
        IGetCurrencyPricesUseCase useCase,
        CancellationToken cancellationToken,
        IReadOnlyList<Currency> currencies,
        Currency quote = Currency.Brl,
        int decimals = 2) =>
        useCase.ExecuteAsync(currencies, quote, decimals, cancellationToken);
}
