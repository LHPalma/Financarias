using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Application.MarketData.Indicators.Specifications;
using Financarias.Domain.MarketData.Indicators;

namespace Financarias.Application.MarketData.Indicators.Commands;

public class ImportIndicatorValuesCommandHandler(
    IIndicatorValueProvider provider,
    IRepository<IndicatorValue> repository
) : ICommandHandler<ImportIndicatorValuesCommand, IndicatorValueImportResult>
{
    public async Task<IndicatorValueImportResult> HandleAsync(
        ImportIndicatorValuesCommand command,
        CancellationToken cancellationToken = default)
    {
        var items = await provider.GetYearAsync(command.Indicator, command.Year, cancellationToken);
        if (items.Count == 0)
        {
            return new IndicatorValueImportResult(0, 0, 0);
        }

        var existing = await repository.ListAsync(
            new IndicatorValuesByYearSpecification(command.Indicator, command.Year),
            cancellationToken);
        var valueByDate = existing.ToDictionary(value => value.ReferenceDate);

        var newValues = new List<IndicatorValue>();
        var updated = 0;

        foreach (var item in items)
        {
            if (!valueByDate.TryGetValue(item.ReferenceDate, out var value))
            {
                value = IndicatorValue.Create(item.Indicator, item.ReferenceDate, item.Value);
                valueByDate[item.ReferenceDate] = value;
                newValues.Add(value);
            }
            else if (value.Value != item.Value)
            {
                value.UpdateValue(item.Value);
                updated++;
            }
        }

        if (newValues.Count > 0)
        {
            await repository.AddRangeAsync(newValues, cancellationToken);
        }
        else if (updated > 0)
        {
            await repository.SaveChangesAsync(cancellationToken);
        }

        return new IndicatorValueImportResult(items.Count, newValues.Count, updated);
    }
}
