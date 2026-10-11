using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Stocks.Commands;
using Financarias.Application.MarketData.Stocks.DTOs.Requests;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.Stocks.UseCases;

public class ImportDailyQuotesUseCase(
    ICommandHandler<ImportDailyQuotesCommand, DailyQuoteImportResult> handler
) : IImportDailyQuotesUseCase
{
    public Task<DailyQuoteImportResult> ExecuteAsync(
        ImportDailyQuotesRequest request,
        CancellationToken cancellationToken = default)
    {
        var period = request switch
        {
            { Day: { } day, Month: null, Year: null } => QuotePeriod.ForDay(day),
            { Month: { } month, Day: null, Year: null } => QuotePeriod.ForMonth(month.Year, month.Month),
            { Year: { } year, Day: null, Month: null } => QuotePeriod.ForYear(year),
            _ => throw MarketDataErrors.Period("exactly one of day, month or year is required"),
        };

        return handler.HandleAsync(new ImportDailyQuotesCommand(period), cancellationToken);
    }
}
