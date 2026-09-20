namespace Financarias.Application.MarketData.Fuel.DTOs.Results;

/// <summary>Preço médio de venda de um combustível, por bandeira e estado.</summary>
/// <param name="Brand">Bandeira dos postos.</param>
/// <param name="State">Sigla da UF.</param>
/// <param name="AveragePrice">Preço médio de venda sobre a última coleta de cada posto, na unidade de medida do produto.</param>
/// <param name="StationCount">Quantidade de postos incluídos na média.</param>
public sealed record BrandAveragePriceResult(
    string Brand,
    string State,
    decimal AveragePrice,
    int StationCount);