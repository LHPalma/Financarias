namespace Financarias.Application.Analytics.Financing.DTOs.Requests;

/// <summary>Dados para simular a quitação antecipada de um financiamento pela Tabela Price.</summary>
/// <param name="Principal">Valor financiado, em reais. Precisa ser positivo.</param>
/// <param name="MonthlyRate">Taxa de juros mensal, em fração (0.015 = 1,5% a.m.). Zero ou mais.</param>
/// <param name="Installments">Número de parcelas do financiamento, de 1 a 600.</param>
/// <param name="AtInstallment">Parcela após cujo pagamento o saldo devedor é quitado, de 1 até o número de parcelas.</param>
public sealed record SimulateEarlyPayoffRequest(
    decimal Principal,
    decimal MonthlyRate,
    int Installments,
    int AtInstallment);
