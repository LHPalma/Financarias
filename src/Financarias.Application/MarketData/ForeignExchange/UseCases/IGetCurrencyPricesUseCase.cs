using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.UseCases;

/// <summary>
///     Consulta o preço atual de uma lista de moedas na moeda de cotação (default BRL),
///     com o número de casas decimais definido pelo chamador (default 2).
/// </summary>
public interface IGetCurrencyPricesUseCase
{
    Task<IReadOnlyList<CurrencyPriceResult>> ExecuteAsync(
        IReadOnlyList<Currency> currencies,
        Currency quote = Currency.Brl,
        int decimals = 2,
        CancellationToken cancellationToken = default);
}
