using System.Globalization;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.Analytics;

/// <summary>Valor nominal de um título (ex.: VNA), em reais. Deve ser positivo.</summary>
public sealed record NominalValue
{
    private NominalValue(decimal value) => Value = value;

    public decimal Value { get; }

    public static NominalValue Create(decimal value)
    {
        return value > 0 ? new NominalValue(value) : throw new InvalidNominalValueException(value);
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
