using Financarias.Domain.MarketData.CorporateEvents;

namespace Financarias.Application.MarketData.CorporateEvents.Import;

public sealed record CorporateEventImportItem(
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
