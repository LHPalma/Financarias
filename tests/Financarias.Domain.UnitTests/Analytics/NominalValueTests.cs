using Financarias.Domain.Analytics;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.UnitTests.Analytics;

public class NominalValueTests
{
    public static TheoryData<decimal> ValidValues => [0.01m, 1m, 4300m, 4321.123456m];

    [Theory(DisplayName = "Create guarda o valor para entradas positivas")]
    [MemberData(nameof(ValidValues))]
    public void Create_WithPositiveValue_KeepsValue(decimal value)
    {
        // Act
        var nominal = NominalValue.Create(value);

        // Assert
        Assert.Equal(value, nominal.Value);
    }

    public static TheoryData<decimal> NonPositiveValues => [0m, -0.01m, -4300m];

    [Theory(DisplayName = "Create lança InvalidNominalValueException para zero ou negativo")]
    [MemberData(nameof(NonPositiveValues))]
    public void Create_WithNonPositiveValue_ThrowsInvalidNominalValueException(decimal value)
    {
        // Act & Assert
        Assert.Throws<InvalidNominalValueException>(() => NominalValue.Create(value));
    }

    [Fact(DisplayName = "Dois valores nominais iguais são iguais (igualdade por valor)")]
    public void Equality_WithSameValue_AreEqual()
    {
        // Arrange
        var a = NominalValue.Create(4300m);
        var b = NominalValue.Create(4300m);

        // Act & Assert
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact(DisplayName = "ToString usa cultura invariante")]
    public void ToString_UsesInvariantCulture()
    {
        // Act & Assert
        Assert.Equal("4321.123456", NominalValue.Create(4321.123456m).ToString());
    }
}
