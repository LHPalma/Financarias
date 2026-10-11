using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Indexes.Commands;
using Financarias.Application.MarketData.Indexes.DTOs.Requests;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Indexes;

namespace Financarias.Application.MarketData.Indexes.UseCases;

public class ImportIndexQuotesUseCase(
    ICommandHandler<ImportIndexQuotesCommand, IndexQuoteImportResult> handler
) : IImportIndexQuotesUseCase
{
    public Task<IndexQuoteImportResult> ExecuteAsync(
        ImportIndexQuotesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Year < IndexQuote.FirstYear)
        {
            throw MarketDataErrors.IndexYear(request.Year);
        }

        return handler.HandleAsync(new ImportIndexQuotesCommand(request.Index, request.Year), cancellationToken);
    }
}
