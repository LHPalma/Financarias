using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.DTOs.Results;
using Financarias.Application.Analytics.Financing.Queries;
using Financarias.Domain.Analytics;
using Financarias.Domain.Analytics.Financing;
using Riok.Mapperly.Abstractions;

namespace Financarias.Application.Analytics.Financing.Mappers;

[Mapper]
public partial class SimulateEarlyPayoffMapper
{
    public partial SimulateEarlyPayoffQuery ToQuery(SimulateEarlyPayoffRequest request);

    public partial EarlyPayoffResult ToResult(EarlyPayoff payoff);

    private static NominalValue ToNominalValue(decimal value) => NominalValue.Create(value);

    private static MonthlyRate ToMonthlyRate(decimal value) => MonthlyRate.FromFraction(value);

    private static InstallmentCount ToInstallmentCount(int value) => InstallmentCount.Create(value);
}
