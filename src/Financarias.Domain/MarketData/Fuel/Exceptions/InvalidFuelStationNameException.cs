using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.MarketData.Fuel.Exceptions;

public sealed class InvalidFuelStationNameException()
    : BaseDomainException("fuel.station.name.required", "Fuel station name is required.");
