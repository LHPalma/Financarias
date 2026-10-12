using System.Text.RegularExpressions;
using Financarias.Domain.Common;

namespace Financarias.Domain.MarketData.IndexComposition;

public sealed partial class IndexCompositionItem : BaseEntity<long>, IAggregateRoot
{
    public const int FirstYear = 2010;
    public const int MaxSourceUrlLength = 500;
    public const int WeightScale = 4;
    public const int DivisorScale = 8;

    [GeneratedRegex("^[A-Z0-9]{2,10}$")]
    private static partial Regex IndexCodePattern();

    [GeneratedRegex("^[A-Z0-9]{4,8}$")]
    private static partial Regex TickerPattern();

    private IndexCompositionItem()
    {
    }

    private IndexCompositionItem(
        string indexCode,
        int year,
        int quadrimester,
        IndexPortfolioKind kind,
        string ticker,
        string assetName,
        string shareType,
        long theoreticalQuantity,
        decimal weightPercent,
        DateOnly? baseDate,
        decimal? divisor,
        string? sourceUrl)
    {
        if (GetError(indexCode, year, quadrimester, ticker, assetName, theoreticalQuantity, weightPercent, divisor, sourceUrl) is { } reason)
        {
            throw MarketDataErrors.IndexComposition(reason);
        }

        IndexCode = indexCode;
        Year = year;
        Quadrimester = quadrimester;
        Kind = kind;
        Ticker = ticker;
        AssetName = assetName;
        ShareType = shareType;
        TheoreticalQuantity = theoreticalQuantity;
        WeightPercent = Math.Round(weightPercent, WeightScale);
        BaseDate = baseDate;
        Divisor = divisor is null ? null : Math.Round(divisor.Value, DivisorScale);
        SourceUrl = sourceUrl;
    }

    public string IndexCode { get; private set; } = null!;

    public int Year { get; private set; }

    public int Quadrimester { get; private set; }

    public IndexPortfolioKind Kind { get; private set; }

    public string Ticker { get; private set; } = null!;

    public string AssetName { get; private set; } = null!;

    public string ShareType { get; private set; } = null!;

    public long TheoreticalQuantity { get; private set; }

    public decimal WeightPercent { get; private set; }

    public DateOnly? BaseDate { get; private set; }

    public decimal? Divisor { get; private set; }

    public string? SourceUrl { get; private set; }

    public static string NormalizeIndexCode(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();

    public static IndexCompositionItem Create(
        string indexCode,
        int year,
        int quadrimester,
        IndexPortfolioKind kind,
        string ticker,
        string assetName,
        string shareType,
        long theoreticalQuantity,
        decimal weightPercent,
        DateOnly? baseDate,
        decimal? divisor,
        string? sourceUrl) =>
        new(
            NormalizeIndexCode(indexCode), year, quadrimester, kind, (ticker ?? string.Empty).Trim().ToUpperInvariant(),
            (assetName ?? string.Empty).Trim(), NormalizeShareType(shareType), theoreticalQuantity, weightPercent, baseDate, divisor, sourceUrl);

    public bool Update(
        string assetName,
        string shareType,
        long theoreticalQuantity,
        decimal weightPercent,
        DateOnly? baseDate,
        decimal? divisor,
        string? sourceUrl)
    {
        if (GetError(IndexCode, Year, Quadrimester, Ticker, assetName, theoreticalQuantity, weightPercent, divisor, sourceUrl) is { } reason)
        {
            throw MarketDataErrors.IndexComposition(reason);
        }

        var name = assetName.Trim();
        var type = NormalizeShareType(shareType);
        var weight = Math.Round(weightPercent, WeightScale);
        var roundedDivisor = divisor is null ? null : (decimal?)Math.Round(divisor.Value, DivisorScale);

        if (AssetName == name && ShareType == type && TheoreticalQuantity == theoreticalQuantity
            && WeightPercent == weight && BaseDate == baseDate && Divisor == roundedDivisor && SourceUrl == sourceUrl)
        {
            return false;
        }

        AssetName = name;
        ShareType = type;
        TheoreticalQuantity = theoreticalQuantity;
        WeightPercent = weight;
        BaseDate = baseDate;
        Divisor = roundedDivisor;
        SourceUrl = sourceUrl;

        return true;
    }

    public static string NormalizeShareType(string? shareType) =>
        string.Join(' ', (shareType ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    public static string? GetError(
        string indexCode,
        int year,
        int quadrimester,
        string ticker,
        string assetName,
        long theoreticalQuantity,
        decimal weightPercent,
        decimal? divisor,
        string? sourceUrl)
    {
        if (!IndexCodePattern().IsMatch(indexCode ?? string.Empty))
        {
            return $"index code must be 2 to 10 capital letters or digits, got '{indexCode}'";
        }

        if (year < FirstYear)
        {
            return $"year must be {FirstYear} or later, got {year}";
        }

        if (quadrimester is < 1 or > 3)
        {
            return $"quadrimester must be 1, 2 or 3, got {quadrimester}";
        }

        if (!TickerPattern().IsMatch((ticker ?? string.Empty).Trim().ToUpperInvariant()))
        {
            return $"ticker must be 4 to 8 capital letters or digits, got '{ticker}'";
        }

        if (string.IsNullOrWhiteSpace(assetName))
        {
            return "asset name is required";
        }

        if (theoreticalQuantity <= 0)
        {
            return $"theoretical quantity must be positive, got {theoreticalQuantity}";
        }

        if (weightPercent is < 0 or > 100)
        {
            return $"weight must be between 0 and 100, got {weightPercent}";
        }

        if (divisor is <= 0)
        {
            return $"divisor must be positive, got {divisor}";
        }

        return sourceUrl is { Length: > MaxSourceUrlLength } ? $"source url is longer than {MaxSourceUrlLength} characters" : null;
    }
}
