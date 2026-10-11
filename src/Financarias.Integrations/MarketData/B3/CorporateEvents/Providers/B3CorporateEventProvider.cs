using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Financarias.Application.MarketData.CorporateEvents.Import;
using Financarias.Domain.MarketData.CorporateEvents;
using Financarias.Integrations.MarketData.B3.CorporateEvents.Clients;
using Financarias.Integrations.MarketData.B3.CorporateEvents.DTOs.Responses;
using Microsoft.Extensions.Logging;
using Refit;

namespace Financarias.Integrations.MarketData.B3.CorporateEvents.Providers;

public class B3CorporateEventProvider(
    IB3CompanyClient client,
    ILogger<B3CorporateEventProvider> logger
) : ICorporateEventProvider
{
    private const int PageSize = 100;
    private const int StoredDecimals = 10;
    private const int MaxShareClassLength = 10;
    private const int MaxLabelLength = 40;

    public async Task<CorporateEventFetch> GetAsync(
        string issuerCode,
        IReadOnlyCollection<string> tradingNames,
        CancellationToken cancellationToken = default)
    {
        var items = new List<CorporateEventImportItem>();
        var skipped = 0;

        foreach (var name in tradingNames.Select(n => n.Trim()).Where(n => n.Length > 0).Distinct())
        {
            foreach (var record in await CashDividendsAsync(name, cancellationToken))
            {
                if (TryMapCash(record, out var item))
                {
                    items.Add(item);
                    continue;
                }

                skipped++;
                logger.LogWarning(
                    "B3 cash event of '{Name}' skipped: {Action} on {Date}, value '{Value}'.",
                    name,
                    record.CorporateAction,
                    record.LastDatePriorEx,
                    record.ValueCash);
            }
        }

        foreach (var record in await StockEventsAsync(issuerCode, cancellationToken))
        {
            if (TryMapStock(record, out var item))
            {
                items.Add(item);
                continue;
            }

            skipped++;
            logger.LogWarning(
                "B3 stock event of {Issuer} skipped: {Label} on {Date}, factor '{Factor}'.",
                issuerCode,
                record.Label,
                record.LastDatePrior,
                record.Factor);
        }

        return new CorporateEventFetch(items, skipped);
    }

    private async Task<List<B3CashDividend>> CashDividendsAsync(string tradingName, CancellationToken cancellationToken)
    {
        var records = new List<B3CashDividend>();

        for (var page = 1; ; page++)
        {
            var payload = Payload(new { language = "pt-br", pageNumber = page, pageSize = PageSize, tradingName });
            var response = await TryAsync(() => client.GetCashDividendsAsync(payload, cancellationToken), tradingName);

            if (response?.Results is not { Count: > 0 } results)
            {
                break;
            }

            records.AddRange(results);

            if (page >= (response.Page?.TotalPages ?? 0))
            {
                break;
            }
        }

        return records;
    }

    private async Task<IEnumerable<B3StockDividend>> StockEventsAsync(string issuerCode, CancellationToken cancellationToken)
    {
        var payload = Payload(new { issuingCompany = issuerCode, language = "pt-br" });
        var companies = await TryAsync(() => client.GetSupplementAsync(payload, cancellationToken), issuerCode);

        return companies?.SelectMany(company => company.StockDividends ?? []) ?? [];
    }

    private async Task<T?> TryAsync<T>(Func<Task<T>> call, string subject)
        where T : class
    {
        try
        {
            return await call();
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotFound)
        {
            logger.LogInformation("B3 has no readable answer for '{Subject}' ({Status}).", subject, (int)ex.StatusCode);
            return null;
        }
    }

    private static string Payload(object body) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)));

    private static bool TryMapCash(B3CashDividend record, out CorporateEventImportItem item)
    {
        item = null!;

        var kind = CashKindOf(record.CorporateAction);
        var shareClass = record.TypeStock?.Trim();
        var cumDate = ParseDate(record.LastDatePriorEx);
        var amount = ParseDecimal(record.ValueCash);
        var lot = ParseDecimal(record.Ratio);

        if (kind is null
            || cumDate is null
            || amount is null or < 0
            || lot is null or <= 0 or > int.MaxValue
            || string.IsNullOrEmpty(shareClass)
            || shareClass.Length > MaxShareClassLength)
        {
            return false;
        }

        item = new CorporateEventImportItem(
            null,
            shareClass,
            kind.Value,
            cumDate.Value,
            ParseDate(record.DateApproval),
            null,
            amount,
            (int)lot.Value,
            null,
            Truncate(record.CorporateAction!.Trim()));
        return true;
    }

    private static bool TryMapStock(B3StockDividend record, out CorporateEventImportItem item)
    {
        item = null!;

        var kind = StockKindOf(record.Label);
        var cumDate = ParseDate(record.LastDatePrior);
        var factor = ParseDecimal(record.Factor);
        var isin = record.IsinCode?.Trim();

        if (kind is null || cumDate is null || factor is null or <= 0 || string.IsNullOrEmpty(isin) || isin.Length > 12)
        {
            return false;
        }

        item = new CorporateEventImportItem(
            isin,
            ShareClassOf(isin),
            kind.Value,
            cumDate.Value,
            ParseDate(record.ApprovedOn),
            null,
            null,
            null,
            factor,
            Truncate(record.Label!.Trim()));
        return true;
    }

    private static CorporateEventKind? CashKindOf(string? action) => action?.Trim().ToUpperInvariant() switch
    {
        "DIVIDENDO" => CorporateEventKind.CashDividend,
        "JRS CAP PROPRIO" => CorporateEventKind.InterestOnEquity,
        "RENDIMENTO" => CorporateEventKind.Yield,
        _ => null,
    };

    private static CorporateEventKind? StockKindOf(string? label) => RemoveDiacritics(label?.Trim() ?? string.Empty).ToUpperInvariant() switch
    {
        "DESDOBRAMENTO" => CorporateEventKind.StockSplit,
        "GRUPAMENTO" => CorporateEventKind.ReverseSplit,
        "BONIFICACAO" => CorporateEventKind.BonusShares,
        _ => null,
    };

    private static string ShareClassOf(string isin)
    {
        if (isin.Length != 12 || isin[6..9] != "ACN")
        {
            return isin.Length > 6 ? isin[6..] : isin;
        }

        return isin[9..11] switch
        {
            "OR" => "ON",
            "PR" => "PN",
            var preferred when preferred[0] == 'P' => "PN" + preferred[1],
            var other => other,
        };
    }

    private static DateOnly? ParseDate(string? text)
    {
        if (!DateOnly.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return null;
        }

        return date.Year is 1900 or 9999 ? null : date;
    }

    private static decimal? ParseDecimal(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = text.Trim().Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.');

        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            ? Math.Round(value, StoredDecimals)
            : null;
    }

    private static string Truncate(string text) => text.Length <= MaxLabelLength ? text : text[..MaxLabelLength];

    private static string RemoveDiacritics(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        {
            builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
