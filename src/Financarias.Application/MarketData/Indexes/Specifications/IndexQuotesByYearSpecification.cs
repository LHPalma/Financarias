using Ardalis.Specification;
using Financarias.Domain.MarketData.Indexes;

namespace Financarias.Application.MarketData.Indexes.Specifications;

public sealed class IndexQuotesByYearSpecification : Specification<IndexQuote>
{
    public IndexQuotesByYearSpecification(MarketIndex marketIndex, int year)
    {
        var firstDay = new DateOnly(year, 1, 1);
        var lastDay = new DateOnly(year, 12, 31);

        Query.Where(quote =>
            quote.MarketIndex == marketIndex && quote.TradingDate >= firstDay && quote.TradingDate <= lastDay);
    }
}
