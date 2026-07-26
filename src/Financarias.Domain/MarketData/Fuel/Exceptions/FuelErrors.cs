using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.MarketData.Fuel.Exceptions;

public static class FuelErrors
{
    public const string PriceInvalid = "fuel.price.invalid";
    public const string StationNameRequired = "fuel.station.name.required";

    public static DomainValidationException Price(decimal salePrice) =>
        new(PriceInvalid, $"Fuel sale price must be positive, got {salePrice}.");

    public static DomainValidationException StationName() =>
        new(StationNameRequired, "Fuel station name is required.");
}
