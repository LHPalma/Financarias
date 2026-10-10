using System.Globalization;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Stocks;

namespace Financarias.Integrations.MarketData.B3.Cotahist;

public static class CotahistLineParser
{
    public const int LineLength = 245;

    private const string QuoteRecord = "01";
    private const string ReferenceCurrency = "R$";

    public static CotahistLineResult TryParse(string line, out DailyQuoteImportItem? item)
    {
        item = null;

        if (!line.StartsWith(QuoteRecord, StringComparison.Ordinal))
        {
            return CotahistLineResult.OutOfScope;
        }

        if (line.Length != LineLength)
        {
            return CotahistLineResult.Skipped;
        }

        if (MapMarketType(Field(line, 25, 27)) is not { } marketType)
        {
            return CotahistLineResult.OutOfScope;
        }

        if (!Field(line, 53, 56).Trim().Equals(ReferenceCurrency, StringComparison.Ordinal))
        {
            return CotahistLineResult.Skipped;
        }

        if (!DateOnly.TryParseExact(Field(line, 3, 10), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tradingDate)
            || !Ticker.TryCreate(Field(line, 13, 24).Trim().ToString(), out var ticker)
            || !TryParseMoney(Field(line, 57, 69), out var openPrice)
            || !TryParseMoney(Field(line, 70, 82), out var highPrice)
            || !TryParseMoney(Field(line, 83, 95), out var lowPrice)
            || !TryParseMoney(Field(line, 96, 108), out var averagePrice)
            || !TryParseMoney(Field(line, 109, 121), out var closePrice)
            || !int.TryParse(Field(line, 148, 152), NumberStyles.None, CultureInfo.InvariantCulture, out var trades)
            || !long.TryParse(Field(line, 153, 170), NumberStyles.None, CultureInfo.InvariantCulture, out var quantity)
            || !TryParseMoney(Field(line, 171, 188), out var volume)
            || !int.TryParse(Field(line, 211, 217), NumberStyles.None, CultureInfo.InvariantCulture, out var quoteFactor))
        {
            return CotahistLineResult.Skipped;
        }

        if (DailyQuote.GetFiguresError(openPrice, highPrice, lowPrice, averagePrice, closePrice, trades, quantity, quoteFactor) is not null)
        {
            return CotahistLineResult.Skipped;
        }

        item = new DailyQuoteImportItem(
            ticker,
            tradingDate,
            marketType,
            Field(line, 11, 12).Trim().ToString(),
            Field(line, 28, 39).Trim().ToString(),
            Field(line, 40, 49).Trim().ToString(),
            Field(line, 231, 242).Trim().ToString(),
            openPrice,
            highPrice,
            lowPrice,
            averagePrice,
            closePrice,
            trades,
            quantity,
            volume,
            quoteFactor);

        return CotahistLineResult.Parsed;
    }

    private static ReadOnlySpan<char> Field(string line, int firstPosition, int lastPosition) =>
        line.AsSpan(firstPosition - 1, lastPosition - firstPosition + 1);

    private static QuoteMarketType? MapMarketType(ReadOnlySpan<char> code) => code switch
    {
        "010" => QuoteMarketType.Cash,
        "020" => QuoteMarketType.Fractional,
        _ => null,
    };

    private static bool TryParseMoney(ReadOnlySpan<char> digits, out decimal value)
    {
        value = 0;

        if (!long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var cents))
        {
            return false;
        }

        value = cents / 100m;
        return true;
    }
}
