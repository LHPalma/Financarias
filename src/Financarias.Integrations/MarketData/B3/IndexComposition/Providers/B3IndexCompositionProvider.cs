using Financarias.Application.MarketData.IndexComposition.Import;
using Financarias.Integrations.MarketData.B3.IndexComposition.Clients;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.MarketData.B3.IndexComposition.Providers;

public class B3IndexCompositionProvider(
    IB3DataFileClient client,
    ILogger<B3IndexCompositionProvider> logger
) : IIndexCompositionProvider
{
    private const string PathPrefix = "/data/files/";

    public async Task<IReadOnlyList<IndexPortfolioImportItem>> GetAsync(
        string sourceUrl,
        CancellationToken cancellationToken = default)
    {
        var uri = new Uri(sourceUrl, UriKind.Absolute);
        var path = Uri.UnescapeDataString(uri.AbsolutePath);
        if (!path.StartsWith(PathPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException($"'{sourceUrl}' is not a B3 data file address.", nameof(sourceUrl));
        }

        await using var download = await client.GetFileAsync(path[PathPrefix.Length..], cancellationToken);
        await using var buffer = new MemoryStream();
        await download.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        return new IndexCompositionWorkbookReader(logger).Read(buffer);
    }
}
