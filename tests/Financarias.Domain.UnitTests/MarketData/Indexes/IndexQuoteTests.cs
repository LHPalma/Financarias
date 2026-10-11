using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData.Indexes;

namespace Financarias.Domain.UnitTests.MarketData.Indexes;

public class IndexQuoteTests
{
    private static readonly DateOnly TradingDate = new(2018, 10, 8);

    [Fact(DisplayName = "Create monta a cotação do índice com pregão, fechamento e origem")]
    public void Create_WithValidData_SetsProperties()
    {
        // Act
        var quote = IndexQuote.Create(MarketIndex.Ibovespa, TradingDate, 86_083.92m, IndexQuoteSource.B3);

        // Assert
        Assert.Equal(MarketIndex.Ibovespa, quote.MarketIndex);
        Assert.Equal(TradingDate, quote.TradingDate);
        Assert.Equal(86_083.92m, quote.CloseValue);
        Assert.Equal(IndexQuoteSource.B3, quote.Source);
    }

    [Theory(DisplayName = "Create lança erro de domínio quando o fechamento não é positivo")]
    [InlineData(0)]
    [InlineData(-1.5)]
    public void Create_WithNonPositiveCloseValue_ThrowsDomainError(double closeValue)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            IndexQuote.Create(MarketIndex.Ibovespa, TradingDate, (decimal)closeValue, IndexQuoteSource.B3));

        Assert.Equal("index.quote.invalid", exception.Code);
    }

    [Fact(DisplayName = "UpdateCloseValue troca o fechamento")]
    public void UpdateCloseValue_WithValidValue_ReplacesIt()
    {
        // Arrange
        var quote = IndexQuote.Create(MarketIndex.Ibovespa, TradingDate, 86_083.00m, IndexQuoteSource.B3);

        // Act
        quote.UpdateCloseValue(86_083.92m);

        // Assert
        Assert.Equal(86_083.92m, quote.CloseValue);
    }

    [Fact(DisplayName = "UpdateCloseValue com valor inválido lança erro e preserva o fechamento")]
    public void UpdateCloseValue_WithInvalidValue_ThrowsAndKeepsPreviousValue()
    {
        // Arrange
        var quote = IndexQuote.Create(MarketIndex.Ibovespa, TradingDate, 86_083.92m, IndexQuoteSource.B3);

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => quote.UpdateCloseValue(0m));

        Assert.Equal("index.quote.invalid", exception.Code);
        Assert.Equal(86_083.92m, quote.CloseValue);
    }
}
