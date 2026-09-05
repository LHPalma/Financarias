namespace Financarias.Application.MarketData.Fuel.DTOs.Results;

public sealed record BrandAveragePriceResult(
    string Brand,
    string State,
    decimal AveragePrice,
    int StationCount);