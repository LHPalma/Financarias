namespace Financarias.Application.Analytics.Financing.DTOs.Results;

/// <summary>Uma linha da tabela de evolução do financiamento.</summary>
/// <param name="Period">Número da parcela, começando em 1.</param>
/// <param name="Installment">Valor da parcela, em reais.</param>
/// <param name="Interest">Juros do período, calculados sobre o saldo devedor anterior.</param>
/// <param name="AccumulatedInterest">Juros acumulados até esta parcela, inclusive.</param>
/// <param name="Amortization">Parte da parcela que abate o saldo devedor: a parcela menos os juros.</param>
/// <param name="OutstandingBalance">Saldo devedor depois de pagar esta parcela.</param>
public record InstallmentBreakdownResult(
    int Period,
    decimal Installment,
    decimal Interest,
    decimal AccumulatedInterest,
    decimal Amortization,
    decimal OutstandingBalance);
