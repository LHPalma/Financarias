using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Application.MarketData.Stocks.Specifications;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Stocks;

namespace Financarias.Application.MarketData.Stocks.Commands;

public class ImportDailyQuotesCommandHandler(
    IDailyQuoteProvider provider,
    IRepository<DailyQuote> repository,
    IUnitOfWork unitOfWork
) : ICommandHandler<ImportDailyQuotesCommand, DailyQuoteImportResult>
{
    private const int BatchSize = 5000;

    public async Task<DailyQuoteImportResult> HandleAsync(
        ImportDailyQuotesCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var file = await provider.OpenAsync(command.Period, cancellationToken);
        if (file is null)
        {
            return new DailyQuoteImportResult(false, 0, 0, 0, 0);
        }

        var totals = default(Counters);
        var batch = new List<DailyQuoteImportItem>(BatchSize);

        await foreach (var item in file.ReadAsync(cancellationToken))
        {
            batch.Add(item);

            if (batch.Count < BatchSize)
            {
                continue;
            }

            totals += await FlushAsync(batch, cancellationToken);
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            totals += await FlushAsync(batch, cancellationToken);
        }

        return new DailyQuoteImportResult(true, totals.Rows, file.RowsSkipped, totals.Created, totals.Updated);
    }

    private static bool TryApplyChanges(DailyQuote quote, DailyQuoteImportItem item)
    {
        if (quote.OpenPrice == item.OpenPrice &&
            quote.HighPrice == item.HighPrice &&
            quote.LowPrice == item.LowPrice &&
            quote.AveragePrice == item.AveragePrice &&
            quote.ClosePrice == item.ClosePrice &&
            quote.Trades == item.Trades &&
            quote.Quantity == item.Quantity &&
            quote.Volume == item.Volume &&
            quote.QuoteFactor == item.QuoteFactor)
        {
            return false;
        }

        quote.UpdateValues(
            item.OpenPrice,
            item.HighPrice,
            item.LowPrice,
            item.AveragePrice,
            item.ClosePrice,
            item.Trades,
            item.Quantity,
            item.Volume,
            item.QuoteFactor);

        return true;
    }

    private static DailyQuote CreateQuote(DailyQuoteImportItem item) =>
        DailyQuote.Create(
            item.Ticker,
            item.TradingDate,
            item.MarketType,
            item.BdiCode,
            item.CompanyName,
            item.Specification,
            item.Isin,
            item.OpenPrice,
            item.HighPrice,
            item.LowPrice,
            item.AveragePrice,
            item.ClosePrice,
            item.Trades,
            item.Quantity,
            item.Volume,
            item.QuoteFactor,
            item.LegacyTicker);

    private async Task<Counters> FlushAsync(
        IReadOnlyList<DailyQuoteImportItem> batch,
        CancellationToken cancellationToken)
    {
        var quoteByKey = await LoadExistingQuotesAsync(batch, cancellationToken);

        var newQuotes = new List<DailyQuote>();
        var updated = 0;

        foreach (var item in batch)
        {
            var key = (item.Ticker, item.TradingDate, item.MarketType);

            if (quoteByKey.TryGetValue(key, out var existing))
            {
                if (TryApplyChanges(existing, item))
                {
                    updated++;
                }
            }
            else
            {
                var created = CreateQuote(item);
                quoteByKey[key] = created;
                newQuotes.Add(created);
            }
        }

        await PersistAsync(newQuotes, updated, cancellationToken);

        unitOfWork.ClearTracking();

        return new Counters(batch.Count, newQuotes.Count, updated);
    }

    private async Task<Dictionary<(Ticker Ticker, DateOnly TradingDate, QuoteMarketType MarketType), DailyQuote>>
        LoadExistingQuotesAsync(
            IReadOnlyList<DailyQuoteImportItem> batch,
            CancellationToken cancellationToken)
    {
        var tickers = batch.Select(item => item.Ticker).Distinct().ToList();
        var tradingDates = batch.Select(item => item.TradingDate).Distinct().ToList();

        var existingQuotes = await repository.ListAsync(
            new QuotesByTickersAndDatesSpecification(tickers, tradingDates),
            cancellationToken);

        return existingQuotes.ToDictionary(quote => (quote.Ticker, quote.TradingDate, quote.MarketType));
    }

    private async Task PersistAsync(
        IReadOnlyList<DailyQuote> newQuotes,
        int updated,
        CancellationToken cancellationToken)
    {
        if (newQuotes.Count > 0)
        {
            await repository.AddRangeAsync(newQuotes, cancellationToken);
        }
        else if (updated > 0)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }
    }

    private readonly record struct Counters(int Rows, int Created, int Updated)
    {
        public static Counters operator +(Counters a, Counters b) =>
            new(a.Rows + b.Rows, a.Created + b.Created, a.Updated + b.Updated);
    }
}
