using Financarias.Domain.MarketData.Indicators;

namespace Financarias.Application.MarketData.Indicators.DTOs.Requests;

/// <summary>
/// Ano de um indicador econômico a importar.
/// </summary>
/// <param name="Indicator">Indicador do Banco Central a importar.</param>
/// <param name="Year">Ano inteiro, a partir de 1980. Um ano em que a série ainda não existia, ou ainda sem valores, importa zero linhas, sem erro.</param>
public sealed record ImportIndicatorValuesRequest(
    EconomicIndicator Indicator,
    int Year);
