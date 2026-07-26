using Financarias.Domain.Analytics.Exceptions;

namespace Financarias.Domain.Analytics.Financing;

public record AmortizationSchedule(
    decimal Installment,
    decimal TotalPaid,
    decimal TotalInterest,
    IReadOnlyList<InstallmentRow> Rows)
{
    public EarlyPayoff PayoffAt(int period)
    {
        if (period < 1 || period > Rows.Count)
        {
            throw AnalyticsErrors.PayoffPeriod(period, Rows.Count);
        }

        var row = Rows[period - 1];

        return new EarlyPayoff(
            Period: period,
            OutstandingBalance: row.OutstandingBalance,
            InstallmentsRemaining: Rows.Count - period,
            InterestPaid: row.AccumulatedInterest,
            InterestSaved: TotalInterest - row.AccumulatedInterest);
    }
}