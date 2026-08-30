namespace Financarias.Application.MarketData.Fuel.DTOs.Results;

public sealed record EthanolGasolineParityResult(
    string StationName,
    string Brand,
    string Municipality,
    string State,
    decimal EthanolPrice,
    decimal GasolinePrice,
    decimal Ratio,
    bool IsEthanolAdvantageous);