using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Fuel.Commands;
using Financarias.Application.MarketData.Fuel.Import;

namespace Financarias.Application.MarketData.Fuel.UseCases;

public class ImportFuelPricesUseCase(
    ICommandHandler<ImportFuelPricesCommand, FuelImportResult> handler
) : IImportFuelPricesUseCase
{
    public Task<FuelImportResult> ExecuteAsync(CancellationToken cancellationToken = default) =>
        handler.HandleAsync(new ImportFuelPricesCommand(), cancellationToken);
}
