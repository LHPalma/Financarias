using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.UseCases;
using Financarias.Domain.MarketData;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class ForeignExchangeQueries
{
    /// <summary>
    ///     Converte um valor entre duas moedas pela cotação mais recente (fonte: er-api). Devolve nulo se alguma das duas moedas não tiver cotação. Não exige autenticação.
    /// </summary>
    /// <param name="amount">Valor a converter, na moeda de origem.</param>
    /// <param name="from">Moeda de origem.</param>
    /// <param name="to">Moeda de destino.</param>
    /// <param name="decimals">Casas decimais do valor convertido, com arredondamento comercial. Padrão: 2.</param>
    [GraphQLName("convertCurrency")]
    public Task<ConversionResult?> ConvertCurrencyAsync(
        IConvertCurrencyUseCase useCase,
        CancellationToken cancellationToken,
        decimal amount,
        Currency from,
        Currency to,
        int decimals = 2) =>
        useCase.ExecuteAsync(amount, from, to, decimals, cancellationToken);

    /// <summary>
    ///     Preço de cada moeda pedida, expresso na moeda de cotação, pela cotação mais recente (fonte: er-api). Moedas sem cotação ficam de fora, e a lista vem vazia se a própria moeda de cotação não tiver cotação. Não exige autenticação.
    /// </summary>
    /// <param name="currencies">Moedas a cotar.</param>
    /// <param name="quote">Moeda em que os preços são expressos. Padrão: BRL.</param>
    /// <param name="decimals">Casas decimais dos preços, com arredondamento comercial. Padrão: 2.</param>
    [GraphQLName("currencyPrices")]
    public Task<IReadOnlyList<CurrencyPriceResult>> GetCurrencyPricesAsync(
        IGetCurrencyPricesUseCase useCase,
        CancellationToken cancellationToken,
        IReadOnlyList<Currency> currencies,
        Currency quote = Currency.Brl,
        int decimals = 2) =>
        useCase.ExecuteAsync(currencies, quote, decimals, cancellationToken);
}
