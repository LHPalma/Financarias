using Financarias.Domain.Common.Exceptions;
﻿using Financarias.Domain.Analytics;

namespace Financarias.Domain.UnitTests.Analytics;

public class InstallmentCountTests
{
    [Theory(DisplayName = "Create guarda o número de parcelas para valores dentro do intervalo")]
    [InlineData(1)]
    [InlineData(48)]
    [InlineData(600)]
    public void Create_WithValidValue_KeepsValue(int value)
    {
        // Act
        var installments = InstallmentCount.Create(value);

        // Assert
        Assert.Equal(value, installments.Value);
    }

    [Theory(DisplayName = "Create lança InvalidInstallmentCountException fora do intervalo 1..600")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(601)]
    public void Create_WithValueOutOfRange_ThrowsInvalidInstallmentCountException(int value)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => InstallmentCount.Create(value));
        Assert.Equal("analytics.installmentcount.invalid", exception.Code);
    }

    [Fact(DisplayName = "Dois InstallmentCounts com o mesmo valor são iguais (igualdade por valor)")]
    public void Equality_WithSameValue_AreEqual()
    {
        // Arrange
        var a = InstallmentCount.Create(48);
        var b = InstallmentCount.Create(48);

        // Act & Assert
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact(DisplayName = "ToString usa cultura invariante")]
    public void ToString_UsesInvariantCulture()
    {
        // Act & Assert
        Assert.Equal("48", InstallmentCount.Create(48).ToString());
    }
}