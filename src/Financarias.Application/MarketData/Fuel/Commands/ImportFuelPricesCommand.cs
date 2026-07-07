using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Fuel.Import;

namespace Financarias.Application.MarketData.Fuel.Commands;

public sealed record ImportFuelPricesCommand : ICommand<FuelImportResult>;
