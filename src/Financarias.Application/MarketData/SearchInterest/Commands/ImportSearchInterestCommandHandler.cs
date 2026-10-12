using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.SearchInterest.Import;
using Financarias.Application.MarketData.SearchInterest.Specifications;
using Financarias.Domain.MarketData.SearchInterest;

namespace Financarias.Application.MarketData.SearchInterest.Commands;

public class ImportSearchInterestCommandHandler(
    ISearchInterestProvider provider,
    IRepository<SearchInterestPoint> repository
) : ICommandHandler<ImportSearchInterestCommand, SearchInterestImportResult>
{
    public async Task<SearchInterestImportResult> HandleAsync(
        ImportSearchInterestCommand command,
        CancellationToken cancellationToken = default)
    {
        var items = await provider.GetAsync(command.Terms, command.Geo, command.From, command.To, cancellationToken);
        if (items.Count == 0)
        {
            return new SearchInterestImportResult(0, 0, 0);
        }

        var key = SearchInterestPoint.ComparisonKeyOf(command.Terms);
        var existing = (await repository.ListAsync(
                new SearchInterestByGroupSpecification(command.Geo, key, command.From, command.To),
                cancellationToken))
            .ToDictionary(point => (point.Term, point.Day));

        var toCreate = new List<SearchInterestPoint>();
        var updated = 0;

        foreach (var item in items)
        {
            var term = SearchInterestPoint.NormalizeTerm(item.Term);

            if (!existing.TryGetValue((term, item.Day), out var point))
            {
                var created = SearchInterestPoint.Create(command.Geo, command.Terms, command.From, command.To, term, item.Day, item.Value);
                existing[(term, item.Day)] = created;
                toCreate.Add(created);
            }
            else if (point.Value != item.Value)
            {
                point.UpdateValue(item.Value);
                updated++;
            }
        }

        if (toCreate.Count > 0)
        {
            await repository.AddRangeAsync(toCreate, cancellationToken);
        }
        else if (updated > 0)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new SearchInterestImportResult(items.Count, toCreate.Count, updated);
    }
}
