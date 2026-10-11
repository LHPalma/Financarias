using Financarias.Application.MarketData.Inflation.Commands;
using Financarias.Application.MarketData.Inflation.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class InflationMutations
{
    /// <summary>
    ///     Recalcula o índice de inflação diário (a tabela que a função `to_present_value` consulta) a partir do IPCA mensal já importado e do calendário de feriados do Brasil, pela metodologia da B3: o fator de cada mês é distribuído geometricamente pelos dias úteis entre dois dias 15 ajustados, e o índice do mês M é atingido no dia 15 ajustado de M+1. Grava só a diferença: níveis novos são criados, os que mudaram são atualizados e os que saíram do intervalo são removidos, então repetir sem mudança nos dados não altera nada.
    ///
    ///     É uma operação de manutenção e hoje não exige autenticação: qualquer chamador pode dispará-la. Deve rodar de novo sempre que o IPCA ou os feriados forem importados, porque o índice gravado é derivado deles e não se atualiza sozinho. Feriados anteriores a 2001 vêm da BrasilAPI e só existem depois de `importHolidays` ter rodado para esses anos.
    ///
    ///     Erro (`extensions.code`): `inflation.series.invalid`, quando não há IPCA importado ou a série tem um mês faltando.
    /// </summary>
    [GraphQLName("rebuildInflationLevels")]
    public Task<InflationLevelsRebuildResult> RebuildInflationLevelsAsync(
        IRebuildInflationLevelsUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);
}
