using System.Text.RegularExpressions;
using Financarias.Domain.Common;

namespace Financarias.Domain.MarketData.SearchInterest;

public sealed partial class SearchInterestPoint : BaseEntity<long>, IAggregateRoot
{
    public const int MaxTerms = 5;
    public const int MaxTermLength = 100;
    public const int MaxDailyRangeDays = 270;
    public const int MaxValue = 100;

    [GeneratedRegex("^[A-Z]{2}(-[A-Z0-9]{1,3})?$")]
    private static partial Regex GeoPattern();

    private SearchInterestPoint()
    {
    }

    private SearchInterestPoint(string geo, string comparisonKey, DateOnly rangeStart, DateOnly rangeEnd, string term, DateOnly day, int value)
    {
        if (GetError(geo, rangeStart, rangeEnd, term, day, value) is { } reason)
        {
            throw MarketDataErrors.SearchInterest(reason);
        }

        Geo = geo;
        ComparisonKey = comparisonKey;
        RangeStart = rangeStart;
        RangeEnd = rangeEnd;
        Term = term;
        Day = day;
        Value = value;
    }

    public string Geo { get; private set; } = null!;

    public string ComparisonKey { get; private set; } = null!;

    public DateOnly RangeStart { get; private set; }

    public DateOnly RangeEnd { get; private set; }

    public string Term { get; private set; } = null!;

    public DateOnly Day { get; private set; }

    public int Value { get; private set; }

    public static string NormalizeTerm(string? term) => (term ?? string.Empty).Trim().ToLowerInvariant();

    public static string ComparisonKeyOf(IEnumerable<string> terms) =>
        string.Join('|', terms.Select(NormalizeTerm).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));

    public static SearchInterestPoint Create(
        string geo,
        IEnumerable<string> comparedTerms,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        string term,
        DateOnly day,
        int value) =>
        new(geo, ComparisonKeyOf(comparedTerms), rangeStart, rangeEnd, NormalizeTerm(term), day, value);

    public void UpdateValue(int value)
    {
        if (value is < 0 or > MaxValue)
        {
            throw MarketDataErrors.SearchInterest($"value must be between 0 and {MaxValue}, got {value}");
        }

        Value = value;
    }

    public static string? GetError(string geo, DateOnly rangeStart, DateOnly rangeEnd, string term, DateOnly day, int value)
    {
        if (!GeoPattern().IsMatch(geo ?? string.Empty))
        {
            return $"geo must be a two-letter country code, optionally with a region (BR, BR-SP), got '{geo}'";
        }

        if (rangeStart > rangeEnd)
        {
            return $"range start {rangeStart:yyyy-MM-dd} is after the end {rangeEnd:yyyy-MM-dd}";
        }

        if (rangeEnd.DayNumber - rangeStart.DayNumber + 1 > MaxDailyRangeDays)
        {
            return $"a daily series covers at most {MaxDailyRangeDays} days";
        }

        if (string.IsNullOrWhiteSpace(term) || term.Length > MaxTermLength)
        {
            return $"term must have 1 to {MaxTermLength} characters";
        }

        if (day < rangeStart || day > rangeEnd)
        {
            return $"day {day:yyyy-MM-dd} is outside the range";
        }

        return value is < 0 or > MaxValue ? $"value must be between 0 and {MaxValue}, got {value}" : null;
    }
}
