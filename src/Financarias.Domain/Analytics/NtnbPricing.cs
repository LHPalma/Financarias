namespace Financarias.Domain.Analytics;

/// <summary>
///     Calcula o Preço Unitário (PU) da NTN-B Principal (Tesouro IPCA+, sem cupom) pela
///     metodologia ANBIMA/Tesouro, truncando em cada etapa:
///     <list type="number">
///         <item><description>Exponencial: <c>(1 + i)^(du/252)</c> — truncada a 14 casas (T-14)</description></item>
///         <item><description>Cotação: <c>100 / Exponencial</c> — truncada a 4 casas (T-4)</description></item>
///         <item><description>PU: <c>VNA × Cotação / 100</c> — truncada a 6 casas (T-6)</description></item>
///     </list>
///     onde <c>i</c> é a taxa anual (forma fracionária, 0.07 = 7%) e <c>du</c> os dias úteis até o vencimento.
/// </summary>
public static class NtnbPricing
{
    private const int BusinessDaysPerYear = 252;

    public static decimal UnitPrice(decimal vna, AnnualYield yield, BusinessDayCount businessDays)
    {
        var quotation = Quotation(yield, businessDays);
        return Truncate(vna * quotation / 100m, 6);
    }

    public static decimal Quotation(AnnualYield yield, BusinessDayCount businessDays)
    {
        var power = (double)businessDays.Value / BusinessDaysPerYear;

        var exponential = Truncate(
            (decimal)Math.Pow((double)(1m + yield.Value), power), 14);

        return Truncate(100m / exponential, 4);
    }

    private static decimal Truncate(decimal value, int decimals)
    {
        var factor = 1m;
        for (var i = 0; i < decimals; i++)
        {
            factor *= 10m;
        }

        return Math.Truncate(value * factor) / factor;
    }
}