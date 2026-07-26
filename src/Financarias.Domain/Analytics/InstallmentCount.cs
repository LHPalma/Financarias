using System.Globalization;
using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.Analytics;

/// <summary>Número de parcelas de um financiamento. Entre 1 e 600 (50 anos).</summary>
public sealed record InstallmentCount
{
    public const int Max = 600;

    private InstallmentCount(int value) => Value = value;

    public int Value { get; }

    public static InstallmentCount Create(int value)
    {
        return value is >= 1 and <= Max
            ? new InstallmentCount(value)
            : throw new InvalidInstallmentCountException(value);
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}