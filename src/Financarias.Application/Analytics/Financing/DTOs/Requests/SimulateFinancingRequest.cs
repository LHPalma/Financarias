namespace Financarias.Application.Analytics.Financing.DTOs.Requests;

public sealed record SimulateFinancingRequest(
    decimal Principal,
    decimal MonthlyRate,
    int Installments);
