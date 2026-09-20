namespace Financarias.Application.Analytics.Financing.DTOs.Results;

/// <summary>Resultado da simulação de quitação antecipada.</summary>
/// <param name="Period">Parcela após cujo pagamento o saldo é quitado.</param>
/// <param name="OutstandingBalance">Saldo devedor a pagar para quitar, depois de pagar a parcela do período.</param>
/// <param name="InstallmentsRemaining">Quantas parcelas deixam de ser pagas.</param>
/// <param name="InterestPaid">Juros já pagos até a parcela do período, inclusive.</param>
/// <param name="InterestSaved">Juros que deixam de ser pagos ao quitar: o total de juros menos os já pagos.</param>
public record EarlyPayoffResult(
    int Period,
    decimal OutstandingBalance,
    int InstallmentsRemaining,
    decimal InterestPaid,
    decimal InterestSaved);
