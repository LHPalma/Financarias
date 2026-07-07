using Financarias.Domain.Geography;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Fuel;
using Financarias.Domain.MarketData.Fuel.Exceptions;

namespace Financarias.Domain.UnitTests.MarketData.Fuel;

public class FuelPriceTests
{
    private static readonly DateOnly CollectedOn = new(2026, 1, 2);

    private static readonly FuelStation Station = FuelStation.Create(
        Cnpj.Create("01.492.748/0003-83"),
        "Posto Copacabana",
        "IPIRANGA",
        Region.North,
        "AC",
        "CRUZEIRO DO SUL",
        null,
        null,
        null,
        null,
        null);

    [Fact(DisplayName = "Create monta o preço com posto, produto, data e valores")]
    public void Create_WithValidData_SetsProperties()
    {
        // Act
        var price = FuelPrice.Create(Station, FuelProduct.Gasoline, CollectedOn, 7.97m, 6.50m, "R$ / litro");

        // Assert
        Assert.Same(Station, price.FuelStation);
        Assert.Equal(Station.Id, price.StationId);
        Assert.Equal(FuelProduct.Gasoline, price.Product);
        Assert.Equal(CollectedOn, price.CollectedOn);
        Assert.Equal(7.97m, price.SalePrice);
        Assert.Equal(6.50m, price.PurchasePrice);
        Assert.Equal("R$ / litro", price.MeasureUnit);
    }

    [Fact(DisplayName = "Create lança ArgumentNullException quando o posto é nulo")]
    public void Create_WithNullStation_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FuelPrice.Create(null!, FuelProduct.Gasoline, CollectedOn, 7.97m, null, "R$ / litro"));
    }

    [Fact(DisplayName = "Create aceita valor de compra nulo (coluna costuma vir vazia no CSV da ANP)")]
    public void Create_WithNullPurchasePrice_SetsNull()
    {
        // Act
        var price = FuelPrice.Create(Station, FuelProduct.Ethanol, CollectedOn, 4.19m, null, "R$ / litro");

        // Assert
        Assert.Null(price.PurchasePrice);
    }

    [Theory(DisplayName = "Create lança InvalidFuelPriceException quando o valor de venda não é positivo")]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void Create_WithNonPositiveSalePrice_ThrowsInvalidFuelPriceException(decimal salePrice)
    {
        // Act & Assert
        Assert.Throws<InvalidFuelPriceException>(() =>
            FuelPrice.Create(Station, FuelProduct.Gasoline, CollectedOn, salePrice, null, "R$ / litro"));
    }

    [Fact(DisplayName = "UpdateValues sobrescreve os valores de uma coleta existente")]
    public void UpdateValues_WithNewData_OverwritesValues()
    {
        // Arrange
        var price = FuelPrice.Create(Station, FuelProduct.Gasoline, CollectedOn, 7.97m, null, "R$ / litro");

        // Act
        price.UpdateValues(8.05m, 6.60m, "R$ / litro");

        // Assert
        Assert.Equal(8.05m, price.SalePrice);
        Assert.Equal(6.60m, price.PurchasePrice);
    }

    [Fact(DisplayName = "UpdateValues lança InvalidFuelPriceException quando o valor de venda não é positivo")]
    public void UpdateValues_WithNonPositiveSalePrice_ThrowsInvalidFuelPriceException()
    {
        // Arrange
        var price = FuelPrice.Create(Station, FuelProduct.Gasoline, CollectedOn, 7.97m, null, "R$ / litro");

        // Act & Assert
        Assert.Throws<InvalidFuelPriceException>(() => price.UpdateValues(0, null, "R$ / litro"));
    }
}
