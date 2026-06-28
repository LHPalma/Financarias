using Financarias.Application.Addresses;
using Financarias.Application.Addresses.UseCases;
using Financarias.Application.Analytics;
using Financarias.Application.Analytics.UseCases;
using Financarias.Application.Holidays.UseCases;
using Financarias.Domain.Holidays.Models;

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

    [GraphQLName("businessDayCount")]
    public Task<int> BusinessDayCountAsync(
        DateOnly start,
        DateOnly end,
        CountryCode countryCode,
        ICountBusinessDaysUseCase useCase,
        CancellationToken cancellationToken)
    {
        return useCase.ExecuteAsync(start, end, countryCode, cancellationToken);
    }
}