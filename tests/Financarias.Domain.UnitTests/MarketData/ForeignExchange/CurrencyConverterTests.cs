using Financarias.Domain.MarketData.ForeignExchange;

namespace Financarias.Domain.UnitTests.MarketData.ForeignExchange;

public class CurrencyConverterTests
{
    [Fact(DisplayName = "Converte triangulando pela base (rates cotados em BRL)")]
    public void Convert_ViaBase_UsesCrossRate()
    {
        // Arrange: base BRL. 1 BRL = 0,20 USD e 0,18 EUR.
        // 100 USD -> BRL -> EUR = 100 * (0,18 / 0,20) = 90
        // Act
        var result = CurrencyConverter.Convert(100m, rateFrom: 0.20m, rateTo: 0.18m);

        // Assert
        Assert.Equal(90m, result);
    }

    [Fact(DisplayName = "Quando o destino é a própria base, usa rate 1")]
    public void Convert_ToBase_ReturnsAmountOverRate()
    {
        // Arrange: 50 USD -> BRL. rateFrom(BRL->USD)=0,20; rateTo(BRL->BRL)=1
        // 50 * (1 / 0,20) = 250 BRL
        // Act
        var result = CurrencyConverter.Convert(50m, rateFrom: 0.20m, rateTo: 1m);

        // Assert
        Assert.Equal(250m, result);
    }
}