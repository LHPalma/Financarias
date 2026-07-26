namespace Financarias.Application.Analytics.Financing.DTOs.Requests;

public sealed record SimulateEarlyPayoffRequest(
    decimal Principal,
    decimal MonthlyRate,
    int Installments,
    int AtInstallment);
