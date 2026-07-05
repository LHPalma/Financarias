using Financarias.Domain.MarketData;

namespace Financarias.Domain.UnitTests.MarketData;

public class TickerTests
{
    [Theory(DisplayName = "Cria ticker normalizando para maiúsculas sem espaços")]
    [InlineData("PETR4", "PETR4")]
    [InlineData("petr4", "PETR4")]
    [InlineData("  petr4  ", "PETR4")]
    [InlineData("bova11", "BOVA11")]
    [InlineData("petr4f", "PETR4F")]
    public void Create_WithValidInput_NormalizesValue(string input, string expected)
    {
        // Act
        var ticker = Ticker.Create(input);

        // Assert
        Assert.Equal(expected, ticker.Value);
    }

    [Theory(DisplayName = "Lança InvalidTickerException para entrada inválida")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("PETR")]        // sem dígito
    [InlineData("PET4")]        // 3 letras
    [InlineData("PETRR4")]      // 5 letras
    [InlineData("PETR123")]     // 3 dígitos
    [InlineData("PETR4X")]      // sufixo inválido
    [InlineData("PETR4FF")]     // sufixo duplicado
    [InlineData("PE TR4")]      // espaço interno
    public void Create_WithInvalidInput_ThrowsInvalidTickerException(string? input)
    {
        // Act & Assert
        Assert.Throws<InvalidTickerException>(() => Ticker.Create(input));
    }

    [Theory(DisplayName = "TryCreate retorna true e produz o ticker para entrada válida")]
    [InlineData("PETR4")]
    [InlineData("petr4")]
    [InlineData("BOVA11")]
    public void TryCreate_WithValidInput_ReturnsTrueAndTicker(string input)
    {
        // Act
        var ok = Ticker.TryCreate(input, out var ticker);

        // Assert
        Assert.True(ok);
        Assert.NotNull(ticker);
    }

    [Theory(DisplayName = "TryCreate retorna false e null para entrada inválida")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("PETR")]
    [InlineData("PETR4X")]
    public void TryCreate_WithInvalidInput_ReturnsFalseAndNull(string? input)
    {
        // Act
        var ok = Ticker.TryCreate(input, out var ticker);

        // Assert
        Assert.False(ok);
        Assert.Null(ticker);
    }

    [Fact(DisplayName = "Dois tickers com o mesmo valor canônico são iguais (igualdade por valor)")]
    public void Equality_WithSameCanonicalValue_AreEqual()
    {
        // Arrange
        var a = Ticker.Create("petr4");
        var b = Ticker.Create("PETR4");

        // Act & Assert
        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact(DisplayName = "ToString retorna o valor canônico")]
    public void ToString_ReturnsCanonicalValue()
    {
        // Act & Assert
        Assert.Equal("PETR4", Ticker.Create("petr4").ToString());
    }
}
