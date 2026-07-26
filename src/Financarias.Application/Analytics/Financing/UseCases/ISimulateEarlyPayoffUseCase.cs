using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.DTOs.Results;

namespace Financarias.Application.Analytics.Financing.UseCases;

/// <summary>
/// Simula a quitação antecipada de um financiamento Price na parcela informada: devolve o saldo
/// devedor a pagar, quantas parcelas restariam, quanto de juro já foi pago e quanto se deixa de
/// pagar quitando ali. Taxa mensal em fração (0.015 = 1,5% a.m.).
/// </summary>
public interface ISimulateEarlyPayoffUseCase
{
    Task<EarlyPayoffResult> ExecuteAsync(
        SimulateEarlyPayoffRequest request,
        CancellationToken cancellationToken = default);
}
