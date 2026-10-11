using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.CorporateEvents.Queries;
using Financarias.Application.MarketData.Issuers.Commands;
using Financarias.Application.MarketData.Issuers.Import;

namespace Financarias.Application.MarketData.Issuers.UseCases;

public class ImportIssuersUseCase(
    IIssuerReads issuerReads,
    ICommandHandler<ImportIssuersCommand, IssuerImportResult> handler
) : IImportIssuersUseCase
{
    public async Task<IssuerImportResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var codes = await issuerReads.IssuerCodesAsync(cancellationToken);
        if (codes.Count == 0)
        {
            return new IssuerImportResult(0, 0, 0, 0);
        }

        return await handler.HandleAsync(new ImportIssuersCommand(codes), cancellationToken);
    }
}
