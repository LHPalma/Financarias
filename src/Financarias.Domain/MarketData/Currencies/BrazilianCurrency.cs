using System.Diagnostics.CodeAnalysis;

namespace Financarias.Domain.MarketData.Currencies;

public static class BrazilianCurrency
{
    public static readonly DateOnly FirstSupportedDate = new(1970, 5, 15);

    public static IReadOnlyList<CurrencyEra> Eras { get; } =
    [
        new("Real", new DateOnly(1994, 7, 1), DateOnly.MaxValue, 1m, "R$"),
        new("Cruzeiro Real", new DateOnly(1993, 8, 1), new DateOnly(1994, 6, 30), 2_750m, "CR$"),
        new("Cruzeiro", new DateOnly(1990, 3, 16), new DateOnly(1993, 7, 31), 2_750_000m, "CR$"),
        new("Cruzado Novo", new DateOnly(1989, 1, 16), new DateOnly(1990, 3, 15), 2_750_000m, "NCZ$"),
        new("Cruzado", new DateOnly(1986, 2, 28), new DateOnly(1989, 1, 15), 2_750_000_000m, "CZ$"),
        new("Cruzeiro", FirstSupportedDate, new DateOnly(1986, 2, 27), 2_750_000_000_000m, "CR$"),
    ];

    public static bool TryGetEra(DateOnly date, [NotNullWhen(true)] out CurrencyEra? era)
    {
        era = Eras.FirstOrDefault(candidate => date >= candidate.From && date <= candidate.To);
        return era is not null;
    }

    public static CurrencyEra EraOf(DateOnly date) =>
        TryGetEra(date, out var era) ? era : throw MarketDataErrors.CurrencyDate(date.ToString("yyyy-MM-dd"));

    public static decimal ToReais(decimal amount, DateOnly date) => amount / EraOf(date).DivisorToReais;

    public static bool IsLegalTenderOn(string modRef, DateOnly date) =>
        TryGetEra(date, out var era) && era.ModRef == modRef;
}
