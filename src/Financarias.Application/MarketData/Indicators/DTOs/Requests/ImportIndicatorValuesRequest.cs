using Financarias.Domain.MarketData.Indicators;

namespace Financarias.Application.MarketData.Indicators.DTOs.Requests;

/// <summary>
/// Ano de um indicador econômico a importar.
/// </summary>
/// <param name="Indicator">Indicador a importar, do Banco Central ou do mercado externo (petróleo, minério de ferro, índice do dólar, VIX).</param>
/// <param name="Year">Ano inteiro, a partir de 1980. Um ano em que a série ainda não existia (o Brent começa em 1987, o índice do dólar em 2006), ou ainda sem valores, importa zero linhas, sem erro.</param>
public sealed record ImportIndicatorValuesRequest(
    EconomicIndicator Indicator,
    int Year);
