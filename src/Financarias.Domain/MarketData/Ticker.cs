using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Financarias.Domain.MarketData;

public sealed partial record Ticker
{
    [GeneratedRegex(@"^[A-Z]{4}\d{1,2}F?$")]
    private static partial Regex TickerPattern();

    private Ticker(string value) => Value = value;

    public string Value { get; }

    public static Ticker Create(string? input)
    {
        return !TryCreate(input, out var ticker)
            ? throw new InvalidTickerException(input)
            : ticker;
    }

    public static bool TryCreate(string? input, [NotNullWhen(true)] out Ticker? ticker)
    {
        ticker = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = Normalize(input);

        if (!TickerPattern().IsMatch(normalized))
        {
            return false;
        }

        ticker = new Ticker(normalized);
        return true;
    }

    public override string ToString() => Value;

    private static string Normalize(string input)
        => input.Trim().ToUpperInvariant();
}