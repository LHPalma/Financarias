using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Application.MarketData.Indexes.Specifications;
using Financarias.Domain.MarketData.Indexes;

namespace Financarias.Application.MarketData.Indexes.Commands;

public class ImportIndexQuotesCommandHandler(
    IIndexQuoteProvider provider,
    IRepository<IndexQuote> repository
) : ICommandHandler<ImportIndexQuotesCommand, IndexQuoteImportResult>
{
    public async Task<IndexQuoteImportResult> HandleAsync(
        ImportIndexQuotesCommand command,
        CancellationToken cancellationToken = default)
    {
        var items = await provider.GetYearAsync(command.MarketIndex, command.Year, cancellationToken);
        if (items.Count == 0)
        {
            return new IndexQuoteImportResult(0, 0, 0);
        }

        var existing = await repository.ListAsync(
            new IndexQuotesByYearSpecification(command.MarketIndex, command.Year),
            cancellationToken);
        var quoteByDate = existing.ToDictionary(quote => quote.TradingDate);

        var newQuotes = new List<IndexQuote>();
        var updated = 0;

        foreach (var item in items)
        {
            if (!quoteByDate.TryGetValue(item.TradingDate, out var quote))
            {
                quote = IndexQuote.Create(item.MarketIndex, item.TradingDate, item.CloseValue, item.Source);
                quoteByDate[item.TradingDate] = quote;
                newQuotes.Add(quote);
            }
            else if (quote.CloseValue != item.CloseValue)
            {
                quote.UpdateCloseValue(item.CloseValue);
                updated++;
            }
        }

        if (newQuotes.Count > 0)
        {
            await repository.AddRangeAsync(newQuotes, cancellationToken);
        }
        else if (updated > 0)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new IndexQuoteImportResult(items.Count, newQuotes.Count, updated);
    }
}
