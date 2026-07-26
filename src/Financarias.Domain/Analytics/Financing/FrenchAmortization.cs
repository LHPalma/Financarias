using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.Analytics.Financing;

/// <summary>
///     Calcula um financiamento pelo Sistema Francês de Amortização (Tabela Price), de parcela
///     constante:
///     <list type="bullet">
///         <item>
///             <description>Parcela: <c>P · i · (1+i)^n / ((1+i)^n − 1)</c>, ou <c>P / n</c> quando <c>i = 0</c></description>
///         </item>
///         <item>
///             <description>Juros do período: sobre o saldo devedor anterior</description>
///         </item>
///         <item>
///             <description>Amortização: <c>parcela − juros</c>, abatida do saldo devedor</description>
///         </item>
///     </list>
///     Parcela e juros são arredondados a 2 casas (arredondamento comercial) e é o valor arredondado
///     que alimenta a tabela — por isso a última parcela absorve o resíduo de centavos e zera o saldo.
/// </summary>
public static class FrenchAmortization
{
    private const int MoneyDecimals = 2;

    public static decimal Installment(NominalValue principal, MonthlyRate rate, InstallmentCount installments)
    {
        if (rate.IsZero)
        {
            return Money(principal.Value / installments.Value);
        }

        try
        {
            var factor = Pow(1m + rate.Value, installments.Value);
            return Money(principal.Value * rate.Value * factor / (factor - 1m));
        }
        catch (OverflowException)
        {
            throw new UnrepresentableFinancingException(rate.Value, installments.Value);
        }
    }

    public static AmortizationSchedule Schedule(NominalValue principal, MonthlyRate rate, InstallmentCount installments)
    {
        var installment = Installment(principal, rate, installments);
        var last = installments.Value;
        var rows = new List<InstallmentRow>(last);

        var balance = principal.Value;
        var totalPaid = 0m;
        var totalInterest = 0m;

        for (var period = 1; period <= last; period++)
        {
            var interest = Money(balance * rate.Value);
            var amortization = period == last ? balance : installment - interest;
            var paid = period == last ? interest + amortization : installment;

            balance -= amortization;
            totalPaid += paid;
            totalInterest += interest;

            rows.Add(new InstallmentRow(period, paid, interest, amortization, balance));
        }

        return new AmortizationSchedule(installment, totalPaid, totalInterest, rows);
    }

    private static decimal Money(decimal value) =>
        decimal.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);

    // A BCL não tem Pow para decimal (Math.Pow só aceita double), e expoente inteiro é o único
    // caso em que a potência é exata. O laço linear é preferido à exponenciação por quadrados
    // porque a ordem das multiplicações muda o resultado nos últimos dígitos.
    private static decimal Pow(decimal value, int exponent)
    {
        var result = 1m;

        for (var i = 0; i < exponent; i++)
        {
            result *= value;
        }

        return result;
    }
}