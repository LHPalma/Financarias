using Financarias.Application.Analytics;
using Financarias.Application.Analytics.DTOs.Requests;
using Financarias.Application.Analytics.DTOs.Results;
using Financarias.Application.Analytics.UseCases;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class NtnbQueries
{
    /// <summary>
    ///     Simula um cenário de compra e venda de NTN-B: calcula o PU de compra e o de venda, cada um na sua taxa e sobre o mesmo prazo, o lucro bruto e a rentabilidade. Não exige autenticação.
    ///
    ///     Taxas em fração (0.07 = 7% a.a.).
    ///
    ///     Erros (`extensions.code`):
    ///     - `analytics.nominalvalue.invalid`: VNA menor ou igual a zero.
    ///     - `analytics.yield.invalid`: taxa menor ou igual a -100%.
    ///     - `analytics.businessdaycount.invalid`: dias úteis negativos.
    /// </summary>
    /// <param name="vna">VNA em reais. Precisa ser positivo.</param>
    /// <param name="buyYield">Taxa anual de compra, em fração (0.07 = 7% a.a.). Precisa ser maior que -1.</param>
    /// <param name="sellYield">Taxa anual de venda, em fração (0.06 = 6% a.a.). Precisa ser maior que -1.</param>
    /// <param name="businessDays">Dias úteis até o vencimento. Zero ou mais.</param>
    [GraphQLName("simulateNtnbScenario")]
    public Task<ScenarioResult> SimulateNtnbScenarioAsync(
        decimal vna,
        decimal buyYield,
        decimal sellYield,
        int businessDays,
        ISimulateScenarioUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(vna, buyYield, sellYield, businessDays, cancellationToken);

    /// <summary>
    ///     Precifica a NTN-B Principal por datas, pela metodologia ANBIMA/STN: liquidação em T+2, VNA projetado para a liquidação, dias úteis até o vencimento e PU. Não exige autenticação.
    ///
    ///     A contagem de dias úteis usa os feriados nacionais já importados (`importHolidays`). Sem importação, só os fins de semana são descontados.
    ///
    ///     Erros (`extensions.code`):
    ///     - `analytics.nominalvalue.invalid`: VNA base menor ou igual a zero.
    ///     - `analytics.yield.invalid`: taxa menor ou igual a -100%.
    /// </summary>
    /// <param name="input">VNA base, taxa, inflação projetada e as datas de negociação e de vencimento.</param>
    [GraphQLName("calculateNtnbPrice")]
    public Task<NtnbPriceResult> CalculateNtnbPriceAsync(
        CalculateNtnbPriceRequest input,
        ICalculateNtnbPriceUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);
}
