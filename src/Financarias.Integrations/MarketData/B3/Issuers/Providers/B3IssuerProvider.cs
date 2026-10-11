using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using Financarias.Application.MarketData.Issuers.Import;
using Financarias.Integrations.MarketData.B3.Issuers.Clients;
using Financarias.Integrations.MarketData.B3.Issuers.DTOs.Responses;
using Microsoft.Extensions.Logging;
using Refit;

namespace Financarias.Integrations.MarketData.B3.Issuers.Providers;

public class B3IssuerProvider(
    IB3IssuerClient client,
    ILogger<B3IssuerProvider> logger
) : IIssuerProvider
{
    private const int PageSize = 120;
    private const int DetailParallelism = 4;
    private const string ClassificationSeparator = " / ";

    public async Task<IssuerFetch> GetAsync(
        IReadOnlyCollection<string> issuerCodes,
        CancellationToken cancellationToken = default)
    {
        var wanted = issuerCodes.ToHashSet(StringComparer.Ordinal);
        var listed = (await ListAsync(cancellationToken))
            .Where(company => company.IssuingCompany is not null && wanted.Contains(company.IssuingCompany))
            .GroupBy(company => company.IssuingCompany!)
            .Select(group => group.First())
            .ToList();

        var items = new ConcurrentBag<IssuerImportItem>();

        await Parallel.ForEachAsync(
            listed,
            new ParallelOptions { MaxDegreeOfParallelism = DetailParallelism, CancellationToken = cancellationToken },
            async (company, token) =>
            {
                var detail = await DetailAsync(company, token);

                if (ToItem(company, detail) is { } item)
                {
                    items.Add(item);
                }
            });

        return new IssuerFetch(items.OrderBy(item => item.IssuerCode, StringComparer.Ordinal).ToList(), wanted.Count - listed.Count);
    }

    private static IssuerImportItem? ToItem(B3ListedCompany company, B3CompanyDetail? detail)
    {
        var companyName = company.CompanyName?.Trim();
        var tradingName = company.TradingName?.Trim();
        var cvmCode = company.CodeCvm?.Trim();

        if (string.IsNullOrEmpty(companyName) || string.IsNullOrEmpty(tradingName) || string.IsNullOrEmpty(cvmCode))
        {
            return null;
        }

        var levels = (detail?.IndustryClassification ?? string.Empty)
            .Split(ClassificationSeparator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return new IssuerImportItem(
            company.IssuingCompany!,
            cvmCode,
            companyName,
            tradingName,
            company.Cnpj,
            levels.ElementAtOrDefault(0),
            levels.ElementAtOrDefault(1),
            levels.ElementAtOrDefault(2),
            NullIfBlank(detail?.Market),
            NullIfBlank(detail?.Activity),
            NullIfBlank(detail?.Website));
    }

    private static string? NullIfBlank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private async Task<List<B3ListedCompany>> ListAsync(CancellationToken cancellationToken)
    {
        var companies = new List<B3ListedCompany>();

        for (var page = 1; ; page++)
        {
            var response = await client.GetInitialCompaniesAsync(
                Payload(new { language = "pt-br", pageNumber = page, pageSize = PageSize }),
                cancellationToken);

            if (response.Results is not { Count: > 0 } results)
            {
                break;
            }

            companies.AddRange(results);

            if (page >= (response.Page?.TotalPages ?? 0))
            {
                break;
            }
        }

        return companies;
    }

    private async Task<B3CompanyDetail?> DetailAsync(B3ListedCompany company, CancellationToken cancellationToken)
    {
        try
        {
            return await client.GetDetailAsync(
                Payload(new { codeCVM = company.CodeCvm, language = "pt-br" }),
                cancellationToken);
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound)
        {
            logger.LogInformation("B3 has no readable detail for issuer {Issuer} ({Status}).", company.IssuingCompany, (int)ex.StatusCode);
            return null;
        }
    }

    private static string Payload(object request) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)));
}
