using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.IndexComposition.Commands;
using Financarias.Application.MarketData.IndexComposition.DTOs.Requests;
using Financarias.Application.MarketData.IndexComposition.Import;
using Financarias.Domain.MarketData;

namespace Financarias.Application.MarketData.IndexComposition.UseCases;

public class ImportIndexCompositionUseCase(
    ICommandHandler<ImportIndexCompositionFileCommand, IndexCompositionFileImportResult> handler,
    IUnitOfWork unitOfWork
) : IImportIndexCompositionUseCase
{
    public const string AllowedHost = "www.b3.com.br";
    public const string AllowedPathPrefix = "/data/files/";

    public async Task<IndexCompositionImportResult> ExecuteAsync(
        ImportIndexCompositionRequest request,
        CancellationToken cancellationToken = default)
    {
        var urls = request.Urls.Select(url => url.Trim()).Where(url => url.Length > 0).Distinct().ToList();

        foreach (var url in urls)
        {
            if (!IsB3DataFile(url))
            {
                throw MarketDataErrors.IndexCompositionSource(url);
            }
        }

        var filesRead = 0;
        var portfolios = 0;
        var rows = 0;
        var created = 0;
        var updated = 0;
        var failures = new List<IndexCompositionImportFailure>();

        foreach (var url in urls)
        {
            try
            {
                var file = await handler.HandleAsync(new ImportIndexCompositionFileCommand(url), cancellationToken);
                filesRead++;
                portfolios += file.Portfolios;
                rows += file.Rows;
                created += file.Created;
                updated += file.Updated;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                unitOfWork.ClearTracking();
                failures.Add(new IndexCompositionImportFailure(url, exception.Message));
            }
        }

        return new IndexCompositionImportResult(filesRead, portfolios, rows, created, updated, failures);
    }

    private static bool IsB3DataFile(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host.Equals(AllowedHost, StringComparison.OrdinalIgnoreCase)
        && uri.AbsolutePath.StartsWith(AllowedPathPrefix, StringComparison.Ordinal);
}
