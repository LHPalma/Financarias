using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Stocks;

namespace Financarias.Domain.UnitTests.MarketData.Stocks;

public class DailyQuoteTests
{
    private static readonly Ticker Petr4 = Ticker.Create("PETR4");

    private static readonly DateOnly TradingDate = new(2026, 10, 9);

    [Fact(DisplayName = "Create monta a cotação com ticker, pregão, mercado, preços e volumes")]
    public void Create_WithValidData_SetsProperties()
    {
        // Act
        var quote = CreateQuote();

        // Assert
        Assert.Equal(Petr4, quote.Ticker);
        Assert.Equal(TradingDate, quote.TradingDate);
        Assert.Equal(QuoteMarketType.Cash, quote.MarketType);
        Assert.Equal("02", quote.BdiCode);
        Assert.Equal("PETROBRAS", quote.CompanyName);
        Assert.Equal("PN      N2", quote.Specification);
        Assert.Equal("BRPETRACNPR6", quote.Isin);
        Assert.Equal(38.10m, quote.OpenPrice);
        Assert.Equal(38.90m, quote.HighPrice);
        Assert.Equal(37.80m, quote.LowPrice);
        Assert.Equal(38.40m, quote.AveragePrice);
        Assert.Equal(38.70m, quote.ClosePrice);
        Assert.Equal(52_310, quote.Trades);
        Assert.Equal(41_250_300, quote.Quantity);
        Assert.Equal(1_583_000_000.55m, quote.Volume);
        Assert.Equal(1, quote.QuoteFactor);
    }

    [Fact(DisplayName = "Create lança ArgumentNullException quando o ticker é nulo")]
    public void Create_WithNullTicker_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DailyQuote.Create(
                null!, TradingDate, QuoteMarketType.Cash, "02", "X", "ON", "BRXXXXACNOR0", 1m, 1m, 1m, 1m, 1m, 1, 1, 1m, 1));
    }

    [Fact(DisplayName = "Create aceita preço médio fora do intervalo mínima–máxima (acontece no arquivo real)")]
    public void Create_WithAverageOutsideRange_IsAccepted()
    {
        // Act
        var quote = CreateQuote(low: 37.80m, high: 38.90m, average: 39.50m);

        // Assert
        Assert.Equal(39.50m, quote.AveragePrice);
    }

    [Theory(DisplayName = "Create lança erro de domínio quando algum preço não é positivo")]
    [InlineData(0, 38.90, 37.80, 38.40, 38.70)]
    [InlineData(38.10, 0, 37.80, 38.40, 38.70)]
    [InlineData(38.10, 38.90, 0, 38.40, 38.70)]
    [InlineData(38.10, 38.90, 37.80, 0, 38.70)]
    [InlineData(38.10, 38.90, 37.80, 38.40, 0)]
    [InlineData(38.10, 38.90, 37.80, 38.40, -1)]
    public void Create_WithNonPositivePrice_ThrowsDomainValidationException(
        double open, double high, double low, double average, double close)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            CreateQuote((decimal)open, (decimal)high, (decimal)low, (decimal)average, (decimal)close));

        Assert.Equal("stock.quote.invalid", exception.Code);
    }

    [Fact(DisplayName = "Create lança erro de domínio quando a máxima é menor que a mínima")]
    public void Create_WithHighBelowLow_ThrowsDomainValidationException()
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => CreateQuote(high: 37.00m, low: 37.80m));
        Assert.Equal("stock.quote.invalid", exception.Code);
    }

    [Theory(DisplayName = "Create lança erro de domínio quando negócios, quantidade ou fator não são positivos")]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(-1, 1, 1)]
    public void Create_WithNonPositiveCounts_ThrowsDomainValidationException(int trades, long quantity, int quoteFactor)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            CreateQuote(trades: trades, quantity: quantity, quoteFactor: quoteFactor));

        Assert.Equal("stock.quote.invalid", exception.Code);
    }

    [Fact(DisplayName = "UpdateValues sobrescreve preços e volumes e mantém a identidade da cotação")]
    public void UpdateValues_WithNewData_OverwritesValuesAndKeepsIdentity()
    {
        // Arrange
        var quote = CreateQuote();

        // Act
        quote.UpdateValues(39m, 40m, 38.5m, 39.2m, 39.9m, 60_000, 50_000_000, 2_000_000_000m, 1);

        // Assert
        Assert.Equal(39m, quote.OpenPrice);
        Assert.Equal(40m, quote.HighPrice);
        Assert.Equal(38.5m, quote.LowPrice);
        Assert.Equal(39.2m, quote.AveragePrice);
        Assert.Equal(39.9m, quote.ClosePrice);
        Assert.Equal(60_000, quote.Trades);
        Assert.Equal(50_000_000, quote.Quantity);
        Assert.Equal(2_000_000_000m, quote.Volume);
        Assert.Equal(Petr4, quote.Ticker);
        Assert.Equal(TradingDate, quote.TradingDate);
        Assert.Equal(QuoteMarketType.Cash, quote.MarketType);
    }

    [Fact(DisplayName = "UpdateValues lança erro de domínio e não altera a cotação quando os dados são inválidos")]
    public void UpdateValues_WithInvalidData_ThrowsAndKeepsPreviousValues()
    {
        // Arrange
        var quote = CreateQuote();

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() =>
            quote.UpdateValues(0m, 40m, 38.5m, 39.2m, 39.9m, 60_000, 50_000_000, 2_000_000_000m, 1));

        Assert.Equal("stock.quote.invalid", exception.Code);
        Assert.Equal(38.10m, quote.OpenPrice);
    }

    [Fact(DisplayName = "GetFiguresError devolve nulo quando os valores são válidos")]
    public void GetFiguresError_WithValidFigures_ReturnsNull()
    {
        // Act
        var error = DailyQuote.GetFiguresError(38.10m, 38.90m, 37.80m, 38.40m, 38.70m, 52_310, 41_250_300, 1);

        // Assert
        Assert.Null(error);
    }

    [Theory(DisplayName = "GetFiguresError descreve o problema quando algum valor é inválido")]
    [InlineData(0, 38.90, 37.80, 38.40, 38.70, 1, 1, 1, "prices must be positive")]
    [InlineData(38.10, 37.00, 37.80, 38.40, 38.70, 1, 1, 1, "is below low price")]
    [InlineData(38.10, 38.90, 37.80, 38.40, 38.70, 0, 1, 1, "trades must be positive")]
    [InlineData(38.10, 38.90, 37.80, 38.40, 38.70, 1, 0, 1, "quantity must be positive")]
    [InlineData(38.10, 38.90, 37.80, 38.40, 38.70, 1, 1, 0, "quote factor must be positive")]
    public void GetFiguresError_WithInvalidFigures_DescribesTheProblem(
        double open, double high, double low, double average, double close, int trades, long quantity, int quoteFactor, string expectedFragment)
    {
        // Act
        var error = DailyQuote.GetFiguresError(
            (decimal)open, (decimal)high, (decimal)low, (decimal)average, (decimal)close, trades, quantity, quoteFactor);

        // Assert
        Assert.NotNull(error);
        Assert.Contains(expectedFragment, error);
    }

    private static DailyQuote CreateQuote(
        decimal open = 38.10m,
        decimal high = 38.90m,
        decimal low = 37.80m,
        decimal average = 38.40m,
        decimal close = 38.70m,
        int trades = 52_310,
        long quantity = 41_250_300,
        int quoteFactor = 1) =>
        DailyQuote.Create(
            Petr4,
            TradingDate,
            QuoteMarketType.Cash,
            "02",
            "PETROBRAS",
            "PN      N2",
            "BRPETRACNPR6",
            open,
            high,
            low,
            average,
            close,
            trades,
            quantity,
            1_583_000_000.55m,
            quoteFactor);
}
