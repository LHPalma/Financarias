using Financarias.Application.Analytics.Financing.DTOs.Results;
using Financarias.Application.Common.Messaging;
using Financarias.Domain.Analytics;

namespace Financarias.Application.Analytics.Financing.Queries;

public sealed record SimulateEarlyPayoffQuery(
    NominalValue Principal,
    MonthlyRate MonthlyRate,
    InstallmentCount Installments,
    int AtInstallment
) : IQuery<EarlyPayoffResult>;
