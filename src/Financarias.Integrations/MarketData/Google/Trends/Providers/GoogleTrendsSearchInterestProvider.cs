using System.Globalization;
using System.Text.Json;
using Financarias.Application.MarketData.SearchInterest.Import;
using Microsoft.Extensions.Logging;
using Financarias.Integrations.MarketData.Google.Trends.Clients;

namespace Financarias.Integrations.MarketData.Google.Trends.Providers;

public class GoogleTrendsSearchInterestProvider(
    IGoogleTrendsClient client,
    ILogger<GoogleTrendsSearchInterestProvider> logger
) : ISearchInterestProvider
{
    private const string Language = "pt-BR";
    private const int TimezoneMinutesWest = 180;
    private const string TimeseriesWidget = "TIMESERIES";

    public async Task<IReadOnlyList<SearchInterestImportItem>> GetAsync(
        IReadOnlyList<string> terms,
        string geo,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        // A sessão (cookie) precisa existir antes da consulta; sem ela o Google responde 429
        await client.OpenSessionAsync(geo, cancellationToken);

        var explore = await client.ExploreAsync(Language, TimezoneMinutesWest, ExploreRequest(terms, geo, from, to), cancellationToken);
        var (request, token) = TimeseriesWidgetOf(explore);

        var timeline = await client.GetTimelineAsync(Language, TimezoneMinutesWest, request, token, cancellationToken);

        return ReadTimeline(timeline, terms);
    }

    private static string ExploreRequest(IReadOnlyList<string> terms, string geo, DateOnly from, DateOnly to) =>
        JsonSerializer.Serialize(new
        {
            comparisonItem = terms.Select(term => new { keyword = term, geo, time = $"{from:yyyy-MM-dd} {to:yyyy-MM-dd}" }),
            category = 0,
            property = string.Empty,
        });

    private static (string Request, string Token) TimeseriesWidgetOf(string body)
    {
        using var document = JsonDocument.Parse(StripGuard(body));

        foreach (var widget in document.RootElement.GetProperty("widgets").EnumerateArray())
        {
            if (widget.GetProperty("id").GetString() == TimeseriesWidget)
            {
                return (widget.GetProperty("request").GetRawText(), widget.GetProperty("token").GetString()!);
            }
        }

        throw new InvalidOperationException("Google Trends answered without a time series widget.");
    }

    private IReadOnlyList<SearchInterestImportItem> ReadTimeline(string body, IReadOnlyList<string> terms)
    {
        using var document = JsonDocument.Parse(StripGuard(body));
        var items = new List<SearchInterestImportItem>();

        foreach (var point in document.RootElement.GetProperty("default").GetProperty("timelineData").EnumerateArray())
        {
            // O último ponto, quando o intervalo chega a hoje, vem parcial e ainda muda
            if (point.TryGetProperty("isPartial", out var partial) && partial.ValueKind == JsonValueKind.True)
            {
                continue;
            }

            if (!long.TryParse(point.GetProperty("time").GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out var epoch))
            {
                logger.LogWarning("Google Trends point with an unreadable time: {Point}", point.GetRawText());
                continue;
            }

            var day = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime);
            var values = point.GetProperty("value");

            for (var i = 0; i < terms.Count && i < values.GetArrayLength(); i++)
            {
                items.Add(new SearchInterestImportItem(terms[i], day, values[i].GetInt32()));
            }
        }

        return items;
    }

    // As respostas começam com um prefixo de proteção, ")]}'", antes do JSON
    private static string StripGuard(string body)
    {
        var start = body.IndexOf('{');

        return start < 0 ? throw new InvalidOperationException("Google Trends answered with something that is not JSON.") : body[start..];
    }
}
