using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.ForeignExchange.DTOs.Results;

/// <summary>Resultado da conversão de um valor entre duas moedas.</summary>
/// <param name="From">Moeda de origem.</param>
/// <param name="To">Moeda de destino.</param>
/// <param name="Amount">Valor informado, na moeda de origem, sem arredondamento.</param>
/// <param name="ConvertedAmount">Valor convertido, arredondado para o número de casas pedido.</param>
/// <param name="Rate">Taxa cruzada: quanto vale 1 unidade da moeda de origem na de destino, sem arredondamento.</param>
/// <param name="AsOf">Instante da cotação usada na conversão.</param>
public sealed record ConversionResult(
    Currency From,
    Currency To,
    decimal Amount,
    decimal ConvertedAmount,
    decimal Rate,
    DateTimeOffset AsOf
);