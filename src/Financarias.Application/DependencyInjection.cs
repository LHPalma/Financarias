using Financarias.Application.Addresses;
using Financarias.Application.Addresses.Queries;
using Financarias.Application.Addresses.UseCases;
using Financarias.Application.Common.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Financarias.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFindAddressByCepUseCase, FindAddressByCepUseCase>();
        services.AddScoped<IQueryHandler<FindAddressByCepQuery, AddressLookupResult?>, FindAddressByCepQueryHandler>();

        return services;
    }
}
