using System.Globalization;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.Analytics;

/// <summary>Taxa mensal de juros em forma fracionária (0.015 = 1,5% a.m.).</summary>
public sealed record MonthlyRate
{
    private MonthlyRate(decimal value) => Value = value;

    public decimal Value { get; }

    public bool IsZero => Value == 0m;

    public static MonthlyRate FromFraction(decimal value) =>
        value >= 0m ? new MonthlyRate(value) : throw AnalyticsErrors.MonthlyRate(value);

    public static MonthlyRate FromPercent(decimal percent) => FromFraction(percent / 100m);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}