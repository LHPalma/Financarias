using Financarias.Domain.Analytics;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.UnitTests.Analytics;

public class AnnualYieldTests
{
    public static TheoryData<decimal> ValidFractions => [0.07m, 0m, -0.05m, 1.5m];

    [Theory(DisplayName = "FromFraction guarda a taxa em forma fracionária")]
    [MemberData(nameof(ValidFractions))]
    public void FromFraction_WithValidValue_KeepsFraction(decimal value)
    {
        // Act
        var yield = AnnualYield.FromFraction(value);

        // Assert
        Assert.Equal(value, yield.Value);
    }

    public static TheoryData<decimal, decimal> PercentToFraction => new()
    {
        { 7m, 0.07m },
        { 6.5m, 0.065m },
        { 0m, 0m },
        { 100m, 1m },
    };

    [Theory(DisplayName = "FromPercent converte percentual para fração dividindo por 100")]
    [MemberData(nameof(PercentToFraction))]
    public void FromPercent_WithPercentValue_ConvertsToFraction(decimal percent, decimal expectedFraction)
    {
        // Act
        var yield = AnnualYield.FromPercent(percent);

        // Assert
        Assert.Equal(expectedFraction, yield.Value);
    }

    public static TheoryData<decimal> InvalidFractions => [-1m, -1.5m, -2m];

    [Theory(DisplayName = "FromFraction lança InvalidYieldException para taxa <= -1 (-100%)")]
    [MemberData(nameof(InvalidFractions))]
    public void FromFraction_WithValueAtOrBelowMinusOne_ThrowsInvalidYieldException(decimal value)
    {
        // Act & Assert
        Assert.Throws<InvalidYieldException>(() => AnnualYield.FromFraction(value));
    }

    [Fact(DisplayName = "FromPercent propaga a invariante: -100% lança InvalidYieldException")]
    public void FromPercent_WithMinusHundredPercent_ThrowsInvalidYieldException()
    {
        // Act & Assert
        Assert.Throws<InvalidYieldException>(() => AnnualYield.FromPercent(-100m));
    }

    [Fact(DisplayName = "Mesma taxa por frações ou percentual produz valores iguais (igualdade por valor)")]
    public void Equality_SameRateViaDifferentFactories_AreEqual()
    {
        // Arrange
        var fromFraction = AnnualYield.FromFraction(0.07m);
        var fromPercent = AnnualYield.FromPercent(7m);

        // Act & Assert
        Assert.Equal(fromFraction, fromPercent);
        Assert.True(fromFraction == fromPercent);
        Assert.Equal(fromFraction.GetHashCode(), fromPercent.GetHashCode());
    }

    [Fact(DisplayName = "ToString usa cultura invariante (ponto decimal)")]
    public void ToString_UsesInvariantCulture()
    {
        // Act & Assert
        Assert.Equal("0.07", AnnualYield.FromFraction(0.07m).ToString());
    }
}