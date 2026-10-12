using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Financarias.Domain.MarketData.IndexComposition;

namespace Financarias.Integrations.MarketData.B3.IndexComposition.Providers;

public sealed partial record IndexCompositionSheetTitle(IndexPortfolioKind Kind, int Year, int Quadrimester)
{
    private static readonly string[] Months =
        ["JANEIRO", "FEVEREIRO", "MARCO", "ABRIL", "MAIO", "JUNHO", "JULHO", "AGOSTO", "SETEMBRO", "OUTUBRO", "NOVEMBRO", "DEZEMBRO"];

    [GeneratedRegex(@"(?<n>[1-3])\s*[ªº]?\s*PREVIA", RegexOptions.IgnoreCase)]
    private static partial Regex PreviewPattern();

    [GeneratedRegex(@"\bVIRADA\b", RegexOptions.IgnoreCase)]
    private static partial Regex TurnPattern();

    [GeneratedRegex(@"\bPARA\b(?<rest>.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex PeriodPattern();

    [GeneratedRegex(@"\b(?<year>20\d{2})\b")]
    private static partial Regex YearPattern();

    public static IndexCompositionSheetTitle? TryParse(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var text = RemoveDiacritics(title).ToUpperInvariant();

        IndexPortfolioKind kind;
        if (PreviewPattern().Match(text) is { Success: true } preview)
        {
            kind = preview.Groups["n"].Value switch
            {
                "1" => IndexPortfolioKind.FirstPreview,
                "2" => IndexPortfolioKind.SecondPreview,
                _ => IndexPortfolioKind.ThirdPreview,
            };
        }
        else if (TurnPattern().IsMatch(text))
        {
            kind = IndexPortfolioKind.Final;
        }
        else
        {
            return null;
        }

        var period = PeriodPattern().Match(text);
        if (!period.Success)
        {
            return null;
        }

        var rest = period.Groups["rest"].Value;
        var month = Months
            .Select((name, index) => (Position: rest.IndexOf(name, StringComparison.Ordinal), Month: index + 1))
            .Where(candidate => candidate.Position >= 0)
            .OrderBy(candidate => candidate.Position)
            .Select(candidate => (int?)candidate.Month)
            .FirstOrDefault();

        if (month is null || YearPattern().Match(rest) is not { Success: true } year)
        {
            return null;
        }

        return new IndexCompositionSheetTitle(kind, int.Parse(year.Groups["year"].Value, CultureInfo.InvariantCulture), ((month.Value - 1) / 4) + 1);
    }

    public static string RemoveDiacritics(string text)
    {
        var builder = new StringBuilder(text.Length);

        foreach (var character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
