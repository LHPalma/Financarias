using System.Globalization;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.Analytics;

/// <summary>Valor monetário nominal, sem moeda associada. Deve ser positivo.</summary>
public sealed record NominalValue
{
    private NominalValue(decimal value) => Value = value;

    public decimal Value { get; }

    public static NominalValue Create(decimal value)
    {
        return value > 0 ? new NominalValue(value) : throw AnalyticsErrors.NominalValue(value);
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
