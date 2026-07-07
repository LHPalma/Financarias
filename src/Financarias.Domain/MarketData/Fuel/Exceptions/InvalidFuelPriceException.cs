using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.MarketData.Fuel.Exceptions;

public sealed class InvalidFuelPriceException(decimal salePrice)
    : BaseDomainException("fuel.price.invalid", $"Fuel sale price must be positive, got {salePrice}.");
