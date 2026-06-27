using Financarias.Domain.Analytics;

namespace Financarias.Domain.UnitTests.Analytics;

public class NtnbPricingTests
{
    // Golden values calculados de forma independente
    public static TheoryData<decimal, decimal, int, decimal> PuGoldenCases => new()
    {
        // vna,        taxa,    du,    PU esperado
        { 4300m,        0.07m,  1250, 3074.0829m },
        { 4300m,        0.06m,  1250, 3220.6484m },
        { 1000m,        0.10m,  2520, 385.543m },
        { 4321.123456m, 0.07m,  1234, 3102.480218m }, // VNA fracionário exercita o trunc-6 do PU
    };

    [Theory(DisplayName = "UnitPrice bate com o PU oficial (golden values da metodologia ANBIMA)")]
    [MemberData(nameof(PuGoldenCases))]
    public void UnitPrice_MatchesAnbimaMethodology_ForKnownCases(decimal vna, decimal yieldFraction, int du, decimal expectedPu)
    {
        // Arrange
        var yield = AnnualYield.FromFraction(yieldFraction);
        var businessDays = BusinessDayCount.Create(du);

        // Act
        var pu = NtnbPricing.UnitPrice(vna, yield, businessDays);

        // Assert
        Assert.Equal(expectedPu, pu);
    }

    [Fact(DisplayName = "du = 0: cotação é 100 e PU é igual ao VNA (caso de borda exato)")]
    public void UnitPrice_WithZeroBusinessDays_EqualsVna()
    {
        // Arrange
        var yield = AnnualYield.FromFraction(0.07m);
        var businessDays = BusinessDayCount.Create(0);

        // Act
        var quotation = NtnbPricing.Quotation(yield, businessDays);
        var pu = NtnbPricing.UnitPrice(4300m, yield, businessDays);

        // Assert
        Assert.Equal(100m, quotation);
        Assert.Equal(4300m, pu);
    }

    [Fact(DisplayName = "Cotação trunca a 4 casas (não arredonda): 76.4535, não 76.4536")]
    public void Quotation_TruncatesToFourDecimals_NotRounds()
    {
        // Arrange — cotação crua = 76.45355908; arredondar a 4 daria 76.4536
        var yield = AnnualYield.FromFraction(0.07m);
        var businessDays = BusinessDayCount.Create(1000);

        // Act
        var quotation = NtnbPricing.Quotation(yield, businessDays);

        // Assert
        Assert.Equal(76.4535m, quotation);
    }

    [Fact(DisplayName = "PU trunca a 6 casas (não arredonda): ...218, não ...219")]
    public void UnitPrice_TruncatesToSixDecimals_NotRounds()
    {
        // Arrange — PU cru = 3102.48021893888; arredondar a 6 daria 3102.480219
        var yield = AnnualYield.FromFraction(0.07m);
        var businessDays = BusinessDayCount.Create(1234);

        // Act
        var pu = NtnbPricing.UnitPrice(4321.123456m, yield, businessDays);

        // Assert
        Assert.Equal(3102.480218m, pu);
    }

    [Fact(DisplayName = "Taxa menor produz PU maior (monotonicidade do desconto)")]
    public void UnitPrice_LowerYield_ProducesHigherPrice()
    {
        // Arrange
        var businessDays = BusinessDayCount.Create(1250);
        var lowerYield = AnnualYield.FromFraction(0.06m);
        var higherYield = AnnualYield.FromFraction(0.07m);

        // Act
        var puLower = NtnbPricing.UnitPrice(4300m, lowerYield, businessDays);
        var puHigher = NtnbPricing.UnitPrice(4300m, higherYield, businessDays);

        // Assert
        Assert.True(puLower > puHigher);
    }
}
