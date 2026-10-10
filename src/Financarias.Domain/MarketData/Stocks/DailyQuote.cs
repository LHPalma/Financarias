using Financarias.Domain.Common;

namespace Financarias.Domain.MarketData.Stocks;

public sealed class DailyQuote : BaseEntity<long>, IAggregateRoot
{
    private DailyQuote()
    {
    }

    private DailyQuote(
        Ticker ticker,
        DateOnly tradingDate,
        QuoteMarketType marketType,
        string bdiCode,
        string companyName,
        string specification,
        string isin,
        decimal openPrice,
        decimal highPrice,
        decimal lowPrice,
        decimal averagePrice,
        decimal closePrice,
        int trades,
        long quantity,
        decimal volume,
        int quoteFactor)
    {
        ArgumentNullException.ThrowIfNull(ticker);

        EnsureValid(openPrice, highPrice, lowPrice, averagePrice, closePrice, trades, quantity, quoteFactor);

        Ticker = ticker;
        TradingDate = tradingDate;
        MarketType = marketType;
        BdiCode = bdiCode;
        CompanyName = companyName;
        Specification = specification;
        Isin = isin;
        OpenPrice = openPrice;
        HighPrice = highPrice;
        LowPrice = lowPrice;
        AveragePrice = averagePrice;
        ClosePrice = closePrice;
        Trades = trades;
        Quantity = quantity;
        Volume = volume;
        QuoteFactor = quoteFactor;
    }

    public Ticker Ticker { get; private set; } = null!;

    public DateOnly TradingDate { get; private set; }

    public QuoteMarketType MarketType { get; private set; }

    public string BdiCode { get; private set; } = null!;

    public string CompanyName { get; private set; } = null!;

    public string Specification { get; private set; } = null!;

    public string Isin { get; private set; } = null!;

    public decimal OpenPrice { get; private set; }

    public decimal HighPrice { get; private set; }

    public decimal LowPrice { get; private set; }

    public decimal AveragePrice { get; private set; }

    public decimal ClosePrice { get; private set; }

    public int Trades { get; private set; }

    public long Quantity { get; private set; }

    public decimal Volume { get; private set; }

    public int QuoteFactor { get; private set; }

    public static DailyQuote Create(
        Ticker ticker,
        DateOnly tradingDate,
        QuoteMarketType marketType,
        string bdiCode,
        string companyName,
        string specification,
        string isin,
        decimal openPrice,
        decimal highPrice,
        decimal lowPrice,
        decimal averagePrice,
        decimal closePrice,
        int trades,
        long quantity,
        decimal volume,
        int quoteFactor) =>
        new(
            ticker,
            tradingDate,
            marketType,
            bdiCode,
            companyName,
            specification,
            isin,
            openPrice,
            highPrice,
            lowPrice,
            averagePrice,
            closePrice,
            trades,
            quantity,
            volume,
            quoteFactor);

    public void UpdateValues(
        decimal openPrice,
        decimal highPrice,
        decimal lowPrice,
        decimal averagePrice,
        decimal closePrice,
        int trades,
        long quantity,
        decimal volume,
        int quoteFactor)
    {
        EnsureValid(openPrice, highPrice, lowPrice, averagePrice, closePrice, trades, quantity, quoteFactor);

        OpenPrice = openPrice;
        HighPrice = highPrice;
        LowPrice = lowPrice;
        AveragePrice = averagePrice;
        ClosePrice = closePrice;
        Trades = trades;
        Quantity = quantity;
        Volume = volume;
        QuoteFactor = quoteFactor;
    }

    public static string? GetFiguresError(
        decimal openPrice,
        decimal highPrice,
        decimal lowPrice,
        decimal averagePrice,
        decimal closePrice,
        int trades,
        long quantity,
        int quoteFactor)
    {
        if (openPrice <= 0 || highPrice <= 0 || lowPrice <= 0 || averagePrice <= 0 || closePrice <= 0)
        {
            return "prices must be positive";
        }

        if (highPrice < lowPrice)
        {
            return $"high price {highPrice} is below low price {lowPrice}";
        }

        if (trades <= 0)
        {
            return $"trades must be positive, got {trades}";
        }

        if (quantity <= 0)
        {
            return $"quantity must be positive, got {quantity}";
        }

        if (quoteFactor <= 0)
        {
            return $"quote factor must be positive, got {quoteFactor}";
        }

        return null;
    }

    private static void EnsureValid(
        decimal openPrice,
        decimal highPrice,
        decimal lowPrice,
        decimal averagePrice,
        decimal closePrice,
        int trades,
        long quantity,
        int quoteFactor)
    {
        if (GetFiguresError(openPrice, highPrice, lowPrice, averagePrice, closePrice, trades, quantity, quoteFactor) is { } reason)
        {
            throw MarketDataErrors.Quote(reason);
        }
    }
}
