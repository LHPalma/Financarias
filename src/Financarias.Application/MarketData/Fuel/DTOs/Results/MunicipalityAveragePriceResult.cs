namespace Financarias.Application.MarketData.Fuel.DTOs.Results;

public sealed record MunicipalityAveragePriceResult(
    string Municipality,
    string State,
    decimal AveragePrice,
    int StationCount);