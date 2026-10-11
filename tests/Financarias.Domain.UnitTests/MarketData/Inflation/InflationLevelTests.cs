using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData.Inflation;

namespace Financarias.Domain.UnitTests.MarketData.Inflation;

public class InflationLevelTests
{
    [Fact(DisplayName = "Create monta o nível do dia")]
    public void Create_SetsTheDayAndTheLevel()
    {
        // Act
        var level = InflationLevel.Create(new DateOnly(2026, 10, 15), 1_010_548_936_213.5m);

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 15), level.Day);
        Assert.Equal(1_010_548_936_213.5m, level.Level);
    }

    [Theory(DisplayName = "Create lança erro de domínio quando o nível não é positivo")]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void Create_WithNonPositiveLevel_ThrowsDomainError(double value)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            InflationLevel.Create(new DateOnly(2026, 10, 15), (decimal)value));

        Assert.Equal("inflation.series.invalid", exception.Code);
    }

    [Fact(DisplayName = "UpdateLevel troca o nível e, se inválido, lança erro e preserva o anterior")]
    public void UpdateLevel_ReplacesTheLevel_AndKeepsItWhenInvalid()
    {
        // Arrange
        var level = InflationLevel.Create(new DateOnly(2026, 10, 15), 2m);

        // Act
        level.UpdateLevel(3m);
        var exception = Assert.Throws<DomainValidationException>(() => level.UpdateLevel(0m));

        // Assert
        Assert.Equal(3m, level.Level);
        Assert.Equal("inflation.series.invalid", exception.Code);
    }
}
