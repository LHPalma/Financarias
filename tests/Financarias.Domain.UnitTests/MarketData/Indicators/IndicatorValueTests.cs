using Financarias.Domain.MarketData.Indicators;

namespace Financarias.Domain.UnitTests.MarketData.Indicators;

public class IndicatorValueTests
{
    private static readonly DateOnly ReferenceDate = new(2026, 10, 9);

    [Fact(DisplayName = "Create monta o valor do indicador com data de referência")]
    public void Create_SetsProperties()
    {
        // Act
        var value = IndicatorValue.Create(EconomicIndicator.Dollar, ReferenceDate, 4.9892m);

        // Assert
        Assert.Equal(EconomicIndicator.Dollar, value.Indicator);
        Assert.Equal(ReferenceDate, value.ReferenceDate);
        Assert.Equal(4.9892m, value.Value);
    }

    [Fact(DisplayName = "Create aceita valor negativo, como a variação mensal do IPCA em deflação")]
    public void Create_AcceptsNegativeValue()
    {
        // Act
        var value = IndicatorValue.Create(EconomicIndicator.Ipca, new DateOnly(2026, 8, 1), -0.32m);

        // Assert
        Assert.Equal(-0.32m, value.Value);
    }

    [Fact(DisplayName = "UpdateValue troca o valor")]
    public void UpdateValue_ReplacesTheValue()
    {
        // Arrange
        var value = IndicatorValue.Create(EconomicIndicator.SelicTarget, ReferenceDate, 13.75m);

        // Act
        value.UpdateValue(13.50m);

        // Assert
        Assert.Equal(13.50m, value.Value);
    }
}
