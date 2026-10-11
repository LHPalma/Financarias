using Financarias.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Financarias.Application.MarketData.CorporateEvents.Queries;

public class IssuerReads(IApplicationDbContext dbContext) : IIssuerReads
{
    public async Task<IReadOnlyList<string>> TradingNamesAsync(
        string issuerCode,
        CancellationToken cancellationToken = default)
    {
        var prefix = "BR" + issuerCode;

        return await dbContext.DailyQuotes
            .Where(quote => quote.Isin.StartsWith(prefix))
            .Select(quote => quote.CompanyName)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
    }
}
