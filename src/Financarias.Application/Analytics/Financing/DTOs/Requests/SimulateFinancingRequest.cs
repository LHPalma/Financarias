namespace Financarias.Application.Analytics.Financing.DTOs.Requests;

/// <summary>Dados para simular um financiamento pela Tabela Price.</summary>
/// <param name="Principal">Valor financiado, em reais. Precisa ser positivo.</param>
/// <param name="MonthlyRate">Taxa de juros mensal, em fração (0.015 = 1,5% a.m.). Zero ou mais; com zero, a parcela é o valor financiado dividido pelo número de parcelas.</param>
/// <param name="Installments">Número de parcelas, de 1 a 600.</param>
public sealed record SimulateFinancingRequest(
    decimal Principal,
    decimal MonthlyRate,
    int Installments);
