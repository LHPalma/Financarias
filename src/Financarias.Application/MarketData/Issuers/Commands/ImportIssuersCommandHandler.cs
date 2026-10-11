using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.Issuers.Import;
using Financarias.Application.MarketData.Issuers.Specifications;
using Financarias.Domain.LegalEntities;
using Financarias.Domain.MarketData.Issuers;

namespace Financarias.Application.MarketData.Issuers.Commands;

public class ImportIssuersCommandHandler(
    IIssuerProvider provider,
    IRepository<Issuer> repository
) : ICommandHandler<ImportIssuersCommand, IssuerImportResult>
{
    public async Task<IssuerImportResult> HandleAsync(
        ImportIssuersCommand command,
        CancellationToken cancellationToken = default)
    {
        var fetch = await provider.GetAsync(command.IssuerCodes, cancellationToken);
        var existing = (await repository.ListAsync(new IssuersByCodesSpecification(command.IssuerCodes), cancellationToken))
            .ToDictionary(issuer => issuer.IssuerCode);

        var toCreate = new List<Issuer>();
        var updated = 0;

        foreach (var item in fetch.Items)
        {
            var cnpj = Cnpj.TryCreate(item.Cnpj, out var parsed) ? parsed : null;

            if (!existing.TryGetValue(item.IssuerCode, out var issuer))
            {
                toCreate.Add(Issuer.Create(
                    item.IssuerCode, item.CvmCode, item.CompanyName, item.TradingName, cnpj,
                    item.Sector, item.Subsector, item.Segment, item.ListingMarket, item.Activity, item.Website));
            }
            else if (issuer.Update(
                         item.CvmCode, item.CompanyName, item.TradingName, cnpj,
                         item.Sector, item.Subsector, item.Segment, item.ListingMarket, item.Activity, item.Website))
            {
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

        return new IssuerImportResult(command.IssuerCodes.Count, fetch.NotListed, toCreate.Count, updated);
    }
}
