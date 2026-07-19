using Financarias.Application.Holidays.Import;
using Financarias.Application.Holidays.UseCases;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Application.MarketData.Fuel.UseCases;

namespace Financarias.Api.GraphQL;

public class Mutation
{
    [GraphQLName("importHolidays")]
    public Task<HolidayImportResult> ImportHolidaysAsync(
        IImportHolidaysUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);

    [GraphQLName("importFuelPrices")]
    public Task<FuelImportResult> ImportFuelPricesAsync(
        IImportFuelPricesUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);
}