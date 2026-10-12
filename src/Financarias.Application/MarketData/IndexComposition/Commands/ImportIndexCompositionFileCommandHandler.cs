using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.IndexComposition.Import;
using Financarias.Application.MarketData.IndexComposition.Specifications;
using Financarias.Domain.MarketData.IndexComposition;

namespace Financarias.Application.MarketData.IndexComposition.Commands;

public class ImportIndexCompositionFileCommandHandler(
    IIndexCompositionProvider provider,
    IRepository<IndexCompositionItem> repository
) : ICommandHandler<ImportIndexCompositionFileCommand, IndexCompositionFileImportResult>
{
    public async Task<IndexCompositionFileImportResult> HandleAsync(
        ImportIndexCompositionFileCommand command,
        CancellationToken cancellationToken = default)
    {
        var portfolios = await provider.GetAsync(command.SourceUrl, cancellationToken);
        if (portfolios.Count == 0)
        {
            return new IndexCompositionFileImportResult(0, 0, 0, 0);
        }

        var years = portfolios.Select(portfolio => portfolio.Year).Distinct().ToList();
        var itemByKey = (await repository.ListAsync(new IndexCompositionByYearsSpecification(years), cancellationToken))
            .ToDictionary(item => (item.IndexCode, item.Year, item.Quadrimester, item.Kind, item.Ticker));

        var newItems = new List<IndexCompositionItem>();
        var updated = 0;
        var rows = 0;

        foreach (var portfolio in portfolios)
        {
            var indexCode = IndexCompositionItem.NormalizeIndexCode(portfolio.IndexCode);

            foreach (var row in portfolio.Rows)
            {
                rows++;
                var key = (indexCode, portfolio.Year, portfolio.Quadrimester, portfolio.Kind, row.Ticker.Trim().ToUpperInvariant());

                if (!itemByKey.TryGetValue(key, out var item))
                {
                    item = IndexCompositionItem.Create(
                        indexCode, portfolio.Year, portfolio.Quadrimester, portfolio.Kind, row.Ticker, row.AssetName, row.ShareType,
                        row.TheoreticalQuantity, row.WeightPercent, portfolio.BaseDate, portfolio.Divisor, command.SourceUrl);
                    itemByKey[key] = item;
                    newItems.Add(item);
                }
                else if (item.Update(
                             row.AssetName, row.ShareType, row.TheoreticalQuantity, row.WeightPercent,
                             portfolio.BaseDate, portfolio.Divisor, command.SourceUrl))
                {
                    updated++;
                }
            }
        }

        if (newItems.Count > 0)
        {
            await repository.AddRangeAsync(newItems, cancellationToken);
        }
        else if (updated > 0)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new IndexCompositionFileImportResult(portfolios.Count, rows, newItems.Count, updated);
    }
}
