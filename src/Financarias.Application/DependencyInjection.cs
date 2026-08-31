using Financarias.Application.Addresses;
using Financarias.Application.Addresses.Queries;
using Financarias.Application.Addresses.UseCases;
using Financarias.Application.Analytics;
using Financarias.Application.Analytics.DTOs.Results;
using Financarias.Application.Analytics.Financing.DTOs.Results;
using Financarias.Application.Analytics.Financing.Mappers;
using Financarias.Application.Analytics.Financing.Queries;
using Financarias.Application.Analytics.Financing.UseCases;
using Financarias.Application.Analytics.Mappers;
using Financarias.Application.Analytics.Queries;
using Financarias.Application.Analytics.UseCases;
using Financarias.Application.Common.Messaging;
using Financarias.Application.Holidays.Queries;
using Financarias.Application.Holidays.UseCases;
using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Application.MarketData.Cryptos.Queries;
using Financarias.Application.MarketData.Cryptos.UseCases;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Queries;
using Financarias.Application.MarketData.ForeignExchange.UseCases;
using Financarias.Application.MarketData.Fuel.Commands;
using Financarias.Application.MarketData.Fuel.DTOs.Results;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Application.MarketData.Fuel.Queries;
using Financarias.Application.MarketData.Fuel.UseCases;
using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Application.MarketData.Stocks.Queries;
using Financarias.Application.MarketData.Stocks.UseCases;
using Financarias.Application.News;
using Financarias.Application.News.Queries;
using Financarias.Application.News.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace Financarias.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IFindAddressByCepUseCase, FindAddressByCepUseCase>();
        services.AddScoped<IQueryHandler<FindAddressByCepQuery, AddressLookupResult?>, FindAddressByCepQueryHandler>();

        services.AddScoped<ISimulateScenarioUseCase, SimulateScenarioUseCase>();
        services.AddScoped<IQueryHandler<SimulateScenarioQuery, ScenarioResult>, SimulateScenarioQueryHandler>();

        services.AddScoped<ICountBusinessDaysUseCase, CountBusinessDaysUseCase>();
        services
            .AddScoped<IQueryHandler<FindHolidaysInRangeQuery, IReadOnlySet<DateOnly>>,
                FindHolidaysInRangeQueryHandler>();

        services.AddScoped<IImportHolidaysUseCase, ImportHolidaysUseCase>();

        services.AddSingleton<CalculateNtnbPriceMapper>();
        services.AddScoped<ICalculateNtnbPriceUseCase, CalculateNtnbPriceUseCase>();
        services.AddScoped<IQueryHandler<CalculateNtnbPriceQuery, NtnbPriceResult>, CalculateNtnbPriceQueryHandler>();

        services.AddSingleton<SimulateFinancingMapper>();
        services.AddScoped<ISimulateFinancingUseCase, SimulateFinancingUseCase>();
        services
            .AddScoped<IQueryHandler<SimulateFinancingQuery, FinancingSimulationResult>,
                SimulateFinancingQueryHandler>();

        services.AddSingleton<SimulateEarlyPayoffMapper>();
        services.AddScoped<ISimulateEarlyPayoffUseCase, SimulateEarlyPayoffUseCase>();
        services
            .AddScoped<IQueryHandler<SimulateEarlyPayoffQuery, EarlyPayoffResult>,
                SimulateEarlyPayoffQueryHandler>();

        services.AddScoped<IGetStockQuoteUseCase, GetStockQuoteUseCase>();
        services.AddScoped<IQueryHandler<GetStockQuoteQuery, StockQuoteResult?>, GetStockQuoteQueryHandler>();

        services.AddScoped<IGetCryptoQuotesUseCase, GetCryptoQuotesUseCase>();
        services
            .AddScoped<IQueryHandler<GetCryptoQuotesQuery, IReadOnlyList<CryptoQuoteResult>>,
                GetCryptoQuotesQueryHandler>();

        services.AddScoped<IGetLatestNewsUseCase, GetLatestNewsUseCase>();
        services.AddScoped<IQueryHandler<GetLatestNewsQuery, IReadOnlyList<NewsArticle>>, GetLatestNewsQueryHandler>();

        services.AddScoped<IConvertCurrencyUseCase, ConvertCurrencyUseCase>();
        services
            .AddScoped<IQueryHandler<ConvertCurrencyQuery, ConversionResult?>, ConvertCurrencyQueryHandler>();

        services.AddScoped<IGetCurrencyPricesUseCase, GetCurrencyPricesUseCase>();
        services
            .AddScoped<IQueryHandler<GetCurrencyPricesQuery, IReadOnlyList<CurrencyPriceResult>>,
                GetCurrencyPricesQueryHandler>();

        services.AddScoped<IImportFuelPricesUseCase, ImportFuelPricesUseCase>();
        services
            .AddScoped<ICommandHandler<ImportFuelPricesCommand, FuelImportResult>,
                ImportFuelPricesCommandHandler>();

        services.AddScoped<IFindEthanolGasolineParityUseCase, FindEthanolGasolineParityUseCase>();
        services
            .AddScoped<IQueryHandler<FindEthanolGasolineParityQuery, IQueryable<EthanolGasolineParityResult>>,
                FindEthanolGasolineParityQueryHandler>();

        services.AddScoped<IFuelReads, FuelReads>();

        return services;
    }
}