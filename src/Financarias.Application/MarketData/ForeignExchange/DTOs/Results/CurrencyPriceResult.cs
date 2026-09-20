using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.DTOs.Results;

/// <summary>Preço de uma moeda em outra.</summary>
/// <param name="Currency">Moeda cotada.</param>
/// <param name="Quote">Moeda em que o preço é expresso.</param>
/// <param name="Price">Quanto vale 1 unidade da moeda cotada, na moeda de cotação, arredondado para o número de casas pedido.</param>
/// <param name="AsOf">Instante da cotação usada.</param>
public sealed record CurrencyPriceResult(
    Currency Currency,
    Currency Quote,
    decimal Price,
    DateTimeOffset AsOf
);
