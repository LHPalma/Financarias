using Financarias.Domain.MarketData.Fuel;

namespace Financarias.Domain.UnitTests.MarketData.Fuel;

public class EthanolGasolineParityTests
{
    [Fact(DisplayName = "Ratio calcula a razão entre o preço do etanol e da gasolina")]
    public void Ratio_WithEthanolAndGasolinePrices_DividesEthanolByGasoline()
    {
        // Act
        var result = EthanolGasolineParity.Ratio(ethanolPrice: 3.50m, gasolinePrice: 5.00m);

        // Assert
        Assert.Equal(0.70m, result);
    }

    [Theory(DisplayName = "Etanol só é vantajoso quando a razão é menor que 70%")]
    [InlineData(0.69, true)]
    [InlineData(0.70, false)]
    [InlineData(0.71, false)]
    public void IsEthanolAdvantageous_ComparesRatioAgainstSeventyPercentThreshold(decimal ratio, bool expected)
    {
        // Act
        var result = EthanolGasolineParity.IsEthanolAdvantageous(ratio);

        // Assert
        Assert.Equal(expected, result);
    }
}
