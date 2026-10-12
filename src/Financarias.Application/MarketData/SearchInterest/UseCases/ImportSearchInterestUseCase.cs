using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.SearchInterest.Commands;
using Financarias.Application.MarketData.SearchInterest.DTOs.Requests;
using Financarias.Application.MarketData.SearchInterest.Import;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.SearchInterest;

namespace Financarias.Application.MarketData.SearchInterest.UseCases;

public class ImportSearchInterestUseCase(
    ICommandHandler<ImportSearchInterestCommand, SearchInterestImportResult> handler
) : IImportSearchInterestUseCase
{
    public Task<SearchInterestImportResult> ExecuteAsync(
        ImportSearchInterestRequest request,
        CancellationToken cancellationToken = default)
    {
        var terms = (request.Terms ?? [])
            .Select(SearchInterestPoint.NormalizeTerm)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (terms.Count is < 1 or > SearchInterestPoint.MaxTerms)
        {
            throw MarketDataErrors.SearchInterest($"a group has 1 to {SearchInterestPoint.MaxTerms} distinct terms, got {terms.Count}");
        }

        foreach (var term in terms)
        {
            if (SearchInterestPoint.GetError(request.Geo, request.From, request.To, term, request.From, 0) is { } reason)
            {
                throw MarketDataErrors.SearchInterest(reason);
            }
        }

        return handler.HandleAsync(new ImportSearchInterestCommand(terms, request.Geo, request.From, request.To), cancellationToken);
    }
}
