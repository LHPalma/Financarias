using Financarias.Domain.Analytics;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.UnitTests.Analytics;

public class MonthlyRateTests
{
    public static TheoryData<decimal> ValidFractions => [0.015m, 0m, 0.0199m, 1m];

    [Theory(DisplayName = "FromFraction guarda a taxa em forma fracionária")]
    [MemberData(nameof(ValidFractions))]
    public void FromFraction_WithValidValue_KeepsFraction(decimal value)
    {
        // Act
        var rate = MonthlyRate.FromFraction(value);

        // Assert
        Assert.Equal(value, rate.Value);
    }

    public static TheoryData<decimal, decimal> PercentToFraction => new()
    {
        { 1.5m, 0.015m },
        { 1.99m, 0.0199m },
        { 0m, 0m },
    };

    [Theory(DisplayName = "FromPercent converte percentual para fração dividindo por 100")]
    [MemberData(nameof(PercentToFraction))]
    public void FromPercent_WithPercentValue_ConvertsToFraction(decimal percent, decimal expectedFraction)
    {
        // Act
        var rate = MonthlyRate.FromPercent(percent);

        // Assert
        Assert.Equal(expectedFraction, rate.Value);
    }

    public static TheoryData<decimal> InvalidFractions => [-0.01m, -1m, -2.5m];

    [Theory(DisplayName = "FromFraction lança InvalidMonthlyRateException para taxa negativa")]
    [MemberData(nameof(InvalidFractions))]
    public void FromFraction_WithNegativeValue_ThrowsInvalidMonthlyRateException(decimal value)
    {
        // Act & Assert
        Assert.Throws<InvalidMonthlyRateException>(() => MonthlyRate.FromFraction(value));
    }

    [Fact(DisplayName = "FromPercent propaga a invariante: percentual negativo lança InvalidMonthlyRateException")]
    public void FromPercent_WithNegativePercent_ThrowsInvalidMonthlyRateException()
    {
        // Act & Assert
        Assert.Throws<InvalidMonthlyRateException>(() => MonthlyRate.FromPercent(-1.5m));
    }

    [Fact(DisplayName = "Taxa zero é válida e se reconhece como zero (promoção sem juros)")]
    public void IsZero_WithZeroRate_ReturnsTrue()
    {
        // Act
        var zero = MonthlyRate.FromFraction(0m);
        var nonZero = MonthlyRate.FromFraction(0.015m);

        // Assert
        Assert.True(zero.IsZero);
        Assert.False(nonZero.IsZero);
    }

    [Fact(DisplayName = "Mesma taxa por frações ou percentual produz valores iguais (igualdade por valor)")]
    public void Equality_SameRateViaDifferentFactories_AreEqual()
    {
        // Arrange
        var fromFraction = MonthlyRate.FromFraction(0.015m);
        var fromPercent = MonthlyRate.FromPercent(1.5m);

        // Act & Assert
        Assert.Equal(fromFraction, fromPercent);
        Assert.True(fromFraction == fromPercent);
        Assert.Equal(fromFraction.GetHashCode(), fromPercent.GetHashCode());
    }

    [Fact(DisplayName = "ToString usa cultura invariante (ponto decimal)")]
    public void ToString_UsesInvariantCulture()
    {
        // Act & Assert
        Assert.Equal("0.015", MonthlyRate.FromFraction(0.015m).ToString());
    }
}