namespace Financarias.Application.MarketData.Fuel.DTOs.Results;

public sealed record StateAveragePriceResult(
    string State,
    decimal AveragePrice,
    int StationCount);