using Ardalis.Specification;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Stocks;

namespace Financarias.Application.MarketData.Stocks.Specifications;

public sealed class QuotesByTickersAndDatesSpecification : Specification<DailyQuote>
{
    public QuotesByTickersAndDatesSpecification(
        IReadOnlyCollection<Ticker> tickers,
        IReadOnlyCollection<DateOnly> tradingDates)
    {
        Query.Where(quote => tickers.Contains(quote.Ticker) && tradingDates.Contains(quote.TradingDate));
    }
}
