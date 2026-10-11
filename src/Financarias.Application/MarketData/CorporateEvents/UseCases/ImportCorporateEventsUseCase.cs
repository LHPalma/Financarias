using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.CorporateEvents.Commands;
using Financarias.Application.MarketData.CorporateEvents.DTOs.Requests;
using Financarias.Application.MarketData.CorporateEvents.Import;
using Financarias.Application.MarketData.CorporateEvents.Queries;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.CorporateEvents;

namespace Financarias.Application.MarketData.CorporateEvents.UseCases;

public class ImportCorporateEventsUseCase(
    IIssuerReads issuerReads,
    ICommandHandler<ImportCorporateEventsCommand, CorporateEventImportResult> handler
) : IImportCorporateEventsUseCase
{
    public async Task<CorporateEventImportResult> ExecuteAsync(
        ImportCorporateEventsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!CorporateEvent.IsValidIssuerCode(request.Issuer))
        {
            throw MarketDataErrors.CorporateEvent($"issuer code must be 4 letters or digits in capitals, got '{request.Issuer}'");
        }

        var tradingNames = await issuerReads.TradingNamesAsync(request.Issuer, cancellationToken);
        if (tradingNames.Count == 0)
        {
            return new CorporateEventImportResult(false, 0, 0, 0, 0, 0);
        }

        return await handler.HandleAsync(
            new ImportCorporateEventsCommand(request.Issuer, tradingNames),
            cancellationToken);
    }
}
