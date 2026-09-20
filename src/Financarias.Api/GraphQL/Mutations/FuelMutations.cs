using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Application.MarketData.Fuel.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class FuelMutations
{
    /// <summary>
    ///     Importa os preços de combustível da ANP (arquivo semestral). Grava por chave natural: o posto pelo CNPJ e o preço pelo conjunto posto, produto e data da coleta, então repetir a importação atualiza em vez de duplicar.
    ///
    ///     É uma operação de manutenção, pesada, e hoje não exige autenticação: qualquer chamador pode dispará-la.
    /// </summary>
    [GraphQLName("importFuelPrices")]
    public Task<FuelImportResult> ImportFuelPricesAsync(
        IImportFuelPricesUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);
}
