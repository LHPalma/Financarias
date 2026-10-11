using Financarias.Domain.Common;

namespace Financarias.Domain.MarketData.Indexes;

public sealed class IndexQuote : BaseEntity<long>, IAggregateRoot
{
    private IndexQuote()
    {
    }

    private IndexQuote(MarketIndex marketIndex, DateOnly tradingDate, decimal closeValue, IndexQuoteSource source)
    {
        EnsureValid(closeValue);

        MarketIndex = marketIndex;
        TradingDate = tradingDate;
        CloseValue = closeValue;
        Source = source;
    }

    public MarketIndex MarketIndex { get; private set; }

    public DateOnly TradingDate { get; private set; }

    public decimal CloseValue { get; private set; }

    public IndexQuoteSource Source { get; private set; }

    public static IndexQuote Create(
        MarketIndex marketIndex,
        DateOnly tradingDate,
        decimal closeValue,
        IndexQuoteSource source) =>
        new(marketIndex, tradingDate, closeValue, source);

    public void UpdateCloseValue(decimal closeValue)
    {
        EnsureValid(closeValue);

        CloseValue = closeValue;
    }

    private static void EnsureValid(decimal closeValue)
    {
        if (closeValue <= 0)
        {
            throw MarketDataErrors.IndexQuote($"close value must be positive, got {closeValue}");
        }
    }
}
