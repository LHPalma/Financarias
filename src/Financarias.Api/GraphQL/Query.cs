using Financarias.Application.Addresses;
using Financarias.Application.Addresses.UseCases;
using Financarias.Application.Analytics;
using Financarias.Application.Analytics.UseCases;

namespace Financarias.Api.GraphQL;

public class Query
{
    [GraphQLName("addressLookup")]
    public Task<AddressLookupResult?> AddressLookupAsync(
        string cep,
        IFindAddressByCepUseCase useCase,
        CancellationToken cancellationToken)
    {
        return useCase.ExecuteAsync(cep, cancellationToken);
    }

    [GraphQLName("simulateNtnbScenario")]
    public Task<ScenarioResult> SimulateNtnbScenarioAsync(
        decimal vna,
        decimal buyYield,
        decimal sellYield,
        int businessDays,
        ISimulateScenarioUseCase useCase,
        CancellationToken cancellationToken)
    {
        return useCase.ExecuteAsync(vna, buyYield, sellYield, businessDays, cancellationToken);
    }
}
