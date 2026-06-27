using Financarias.Domain.Analytics;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.UnitTests.Analytics;

public class BusinessDayCountTests
{
    public static TheoryData<int> ValidCounts => [0, 1, 21, 1250];

    [Theory(DisplayName = "Create guarda a quantidade de dias úteis para valores >= 0")]
    [MemberData(nameof(ValidCounts))]
    public void Create_WithNonNegativeValue_KeepsValue(int value)
    {
        // Act
        var count = BusinessDayCount.Create(value);

        // Assert
        Assert.Equal(value, count.Value);
    }

    [Fact(DisplayName = "Create aceita zero (limite da invariante)")]
    public void Create_WithZero_IsAllowed()
    {
        // Act
        var count = BusinessDayCount.Create(0);

        // Assert
        Assert.Equal(0, count.Value);
    }

    public static TheoryData<int> NegativeCounts => [-1, -21, int.MinValue];

    [Theory(DisplayName = "Create lança InvalidBusinessDayCountException para valores negativos")]
    [MemberData(nameof(NegativeCounts))]
    public void Create_WithNegativeValue_ThrowsInvalidBusinessDayCountException(int value)
    {
        // Act & Assert
        Assert.Throws<InvalidBusinessDayCountException>(() => BusinessDayCount.Create(value));
    }

    [Fact(DisplayName = "Dois counts com o mesmo valor são iguais (igualdade por valor)")]
    public void Equality_WithSameValue_AreEqual()
    {
        // Arrange
        var a = BusinessDayCount.Create(252);
        var b = BusinessDayCount.Create(252);

        // Act & Assert
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact(DisplayName = "ToString usa cultura invariante")]
    public void ToString_UsesInvariantCulture()
    {
        // Act & Assert
        Assert.Equal("1250", BusinessDayCount.Create(1250).ToString());
    }
}
