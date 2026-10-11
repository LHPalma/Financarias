using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.CorporateEvents.Import;

namespace Financarias.Application.MarketData.CorporateEvents.Commands;

public sealed record ImportCorporateEventsCommand(string IssuerCode, IReadOnlyList<string> TradingNames)
    : ICommand<CorporateEventImportResult>;
