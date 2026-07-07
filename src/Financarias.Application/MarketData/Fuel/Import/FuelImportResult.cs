namespace Financarias.Application.MarketData.Fuel.Import;

public sealed record FuelImportResult(
    int RowsProcessed,
    int StationsCreated,
    int PricesCreated,
    int PricesUpdated);
