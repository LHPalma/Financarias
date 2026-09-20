using Financarias.Application.Analytics.Financing.DTOs.Requests;
using Financarias.Application.Analytics.Financing.DTOs.Results;
using Financarias.Application.Analytics.Financing.UseCases;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class FinancingQueries
{
    /// <summary>
    ///     Simula um financiamento pela Tabela Price (Sistema Francês): parcela fixa, totais e a tabela mês a mês. Não exige autenticação.
    ///
    ///     Taxa mensal em fração (0.015 = 1,5% a.m.). Parcela e juros são arredondados a 2 casas.
    ///
    ///     Erros (`extensions.code`):
    ///     - `analytics.nominalvalue.invalid`: valor financiado menor ou igual a zero.
    ///     - `analytics.monthlyrate.invalid`: taxa mensal negativa.
    ///     - `analytics.installmentcount.invalid`: número de parcelas fora do intervalo de 1 a 600.
    ///     - `analytics.financing.unrepresentable`: o resultado excede o que o cálculo decimal consegue representar.
    /// </summary>
    /// <param name="input">Valor financiado, taxa mensal e número de parcelas.</param>
    [GraphQLName("simulateFinancing")]
    public Task<FinancingSimulationResult> SimulateFinancingAsync(
        SimulateFinancingRequest input,
        ISimulateFinancingUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);

    /// <summary>
    ///     Simula quitar um financiamento da Tabela Price logo após pagar uma parcela: devolve o saldo devedor a pagar, as parcelas que deixam de ser pagas e os juros já pagos e economizados. Não exige autenticação.
    ///
    ///     Taxa mensal em fração (0.015 = 1,5% a.m.).
    ///
    ///     Erros (`extensions.code`):
    ///     - `analytics.nominalvalue.invalid`: valor financiado menor ou igual a zero.
    ///     - `analytics.monthlyrate.invalid`: taxa mensal negativa.
    ///     - `analytics.installmentcount.invalid`: número de parcelas fora do intervalo de 1 a 600.
    ///     - `analytics.financing.unrepresentable`: o resultado excede o que o cálculo decimal consegue representar.
    ///     - `analytics.financing.payoffperiod.invalid`: parcela de quitação fora do intervalo de 1 até o número de parcelas.
    /// </summary>
    /// <param name="input">Dados do financiamento e a parcela em que ele é quitado.</param>
    [GraphQLName("simulateEarlyPayoff")]
    public Task<EarlyPayoffResult> SimulateEarlyPayoffAsync(
        SimulateEarlyPayoffRequest input,
        ISimulateEarlyPayoffUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);
}
