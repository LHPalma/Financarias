using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.CorporateEvents.Import;
using Financarias.Application.MarketData.CorporateEvents.Specifications;
using Financarias.Domain.MarketData.CorporateEvents;

namespace Financarias.Application.MarketData.CorporateEvents.Commands;

public class ImportCorporateEventsCommandHandler(
    ICorporateEventProvider provider,
    IRepository<CorporateEvent> repository
) : ICommandHandler<ImportCorporateEventsCommand, CorporateEventImportResult>
{
    public async Task<CorporateEventImportResult> HandleAsync(
        ImportCorporateEventsCommand command,
        CancellationToken cancellationToken = default)
    {
        var fetch = await provider.GetAsync(command.IssuerCode, command.TradingNames, cancellationToken);
        var existing = await repository.ListAsync(
            new CorporateEventsByIssuerSpecification(command.IssuerCode),
            cancellationToken);

        if (fetch.Items.Count == 0)
        {
            return new CorporateEventImportResult(true, command.TradingNames.Count, 0, fetch.Skipped, 0, 0);
        }

        var (toCreate, toRemove) = Diff(command.IssuerCode, existing, fetch.Items);

        if (toCreate.Count > 0)
        {
            await repository.AddRangeAsync(toCreate, cancellationToken);
        }

        if (toRemove.Count > 0)
        {
            await repository.DeleteRangeAsync(toRemove, cancellationToken);
        }

        return new CorporateEventImportResult(
            true,
            command.TradingNames.Count,
            fetch.Items.Count,
            fetch.Skipped,
            toCreate.Count,
            toRemove.Count);
    }

    private static (List<CorporateEvent> ToCreate, List<CorporateEvent> ToRemove) Diff(
        string issuerCode,
        IReadOnlyList<CorporateEvent> existing,
        IReadOnlyList<CorporateEventImportItem> items)
    {
        var leftovers = existing
            .GroupBy(KeyOf)
            .ToDictionary(group => group.Key, group => new Queue<CorporateEvent>(group));

        var toCreate = new List<CorporateEvent>();

        foreach (var item in items)
        {
            if (leftovers.TryGetValue(KeyOf(item), out var queue) && queue.Count > 0)
            {
                queue.Dequeue();
                continue;
            }

            toCreate.Add(CorporateEvent.Create(
                issuerCode,
                item.Isin,
                item.ShareClass,
                item.Kind,
                item.CumDate,
                item.ApprovalDate,
                item.PaymentDate,
                item.Amount,
                item.PerShares,
                item.Factor,
                item.Label));
        }

        var toRemove = leftovers.Values.SelectMany(queue => queue).ToList();

        return (toCreate, toRemove);
    }

    private static EventKey KeyOf(CorporateEvent corporateEvent) =>
        new(
            corporateEvent.Isin,
            corporateEvent.ShareClass,
            corporateEvent.Kind,
            corporateEvent.CumDate,
            corporateEvent.ApprovalDate,
            corporateEvent.PaymentDate,
            corporateEvent.Amount,
            corporateEvent.PerShares,
            corporateEvent.Factor,
            corporateEvent.Label);

    private static EventKey KeyOf(CorporateEventImportItem item) =>
        new(
            item.Isin,
            item.ShareClass,
            item.Kind,
            item.CumDate,
            item.ApprovalDate,
            item.PaymentDate,
            item.Amount,
            item.PerShares,
            item.Factor,
            item.Label);

    private readonly record struct EventKey(
        string? Isin,
        string ShareClass,
        CorporateEventKind Kind,
        DateOnly CumDate,
        DateOnly? ApprovalDate,
        DateOnly? PaymentDate,
        decimal? Amount,
        int? PerShares,
        decimal? Factor,
        string Label);
}
