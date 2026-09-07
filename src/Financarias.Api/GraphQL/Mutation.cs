using Financarias.Application.Holidays.Import;
using Financarias.Application.Holidays.UseCases;
using Financarias.Application.Identity.Users.DTOs.Requests;
using Financarias.Application.Identity.Users.DTOs.Results;
using Financarias.Application.Identity.Users.UseCases;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Application.MarketData.Fuel.UseCases;

namespace Financarias.Api.GraphQL;

public class Mutation
{
    [GraphQLName("importHolidays")]
    public Task<HolidayImportResult> ImportHolidaysAsync(
        IImportHolidaysUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);

    [GraphQLName("createUser")]
    public Task<UserResult> CreateUserAsync(
        CreateUserRequest input,
        ICreateUserUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);

    [GraphQLName("deactivateUser")]
    public Task<UserResult> DeactivateUserAsync(
        Guid id,
        IDeactivateUserUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(id, cancellationToken);

    [GraphQLName("importFuelPrices")]
    public Task<FuelImportResult> ImportFuelPricesAsync(
        IImportFuelPricesUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cancellationToken);
}