namespace Financarias.Domain.Analytics;

internal static class Truncation
{
    public static decimal Truncate(decimal value, int decimals)
    {
        var factor = 1m;

        for (var i = 0; i < decimals; i++)
        {
            factor *= 10m;
        }

        return decimal.Truncate(value * factor) / factor;
    }
}