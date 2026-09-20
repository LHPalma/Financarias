namespace Financarias.Domain.MarketData.Fuel;

/// <summary>Combustível, como classificado pela ANP.</summary>
public enum FuelProduct
{
    /// <summary>Gasolina comum.</summary>
    Gasoline,

    /// <summary>Gasolina aditivada.</summary>
    PremiumGasoline,

    /// <summary>Etanol.</summary>
    Ethanol,

    /// <summary>Óleo diesel.</summary>
    Diesel,

    /// <summary>Óleo diesel S10.</summary>
    DieselS10,

    /// <summary>GNV (gás natural veicular).</summary>
    Cng,
}
