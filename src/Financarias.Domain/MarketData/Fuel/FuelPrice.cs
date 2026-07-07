using Financarias.Domain.Common;
using Financarias.Domain.MarketData.Fuel.Exceptions;

namespace Financarias.Domain.MarketData.Fuel;

public sealed class FuelPrice : BaseEntity<long>, IAggregateRoot
{
    private FuelPrice()
    {
    }

    private FuelPrice(
        FuelStation station,
        FuelProduct product,
        DateOnly collectedOn,
        decimal salePrice,
        decimal? purchasePrice,
        string measureUnit)
    {
        ArgumentNullException.ThrowIfNull(station);

        if (salePrice <= 0)
        {
            throw new InvalidFuelPriceException(salePrice);
        }

        FuelStation = station;
        StationId = station.Id;
        Product = product;
        CollectedOn = collectedOn;
        SalePrice = salePrice;
        PurchasePrice = purchasePrice;
        MeasureUnit = measureUnit;
    }

    public FuelStation FuelStation { get; private set; } = null!;

    public long StationId { get; private set; }

    public FuelProduct Product { get; private set; }

    public DateOnly CollectedOn { get; private set; }

    public decimal SalePrice { get; private set; }

    public decimal? PurchasePrice { get; private set; }

    public string MeasureUnit { get; private set; } = null!;

    public static FuelPrice Create(
        FuelStation station,
        FuelProduct product,
        DateOnly collectedOn,
        decimal salePrice,
        decimal? purchasePrice,
        string measureUnit) =>
        new(station, product, collectedOn, salePrice, purchasePrice, measureUnit);

    public void UpdateValues(decimal salePrice, decimal? purchasePrice, string measureUnit)
    {
        if (salePrice <= 0)
        {
            throw new InvalidFuelPriceException(salePrice);
        }

        SalePrice = salePrice;
        PurchasePrice = purchasePrice;
        MeasureUnit = measureUnit;
    }
}
