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
using Financarias.Application.Common.Security;
using Financarias.Application.Identity.Users.Commands;
using Financarias.Application.Identity.Users.Mappers;
using Financarias.Application.Identity.Users.Queries;
using Financarias.Application.Identity.Users.UseCases;
using Financarias.Application.Holidays.Queries;
using Financarias.Application.Holidays.UseCases;
using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Application.MarketData.Cryptos.Queries;
using Financarias.Application.MarketData.Cryptos.UseCases;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Queries;
using Financarias.Application.MarketData.ForeignExchange.UseCases;
using Financarias.Application.MarketData.CorporateEvents.Commands;
using Financarias.Application.MarketData.CorporateEvents.Import;
using Financarias.Application.MarketData.CorporateEvents.Queries;
using Financarias.Application.MarketData.CorporateEvents.UseCases;
using Financarias.Application.MarketData.Fuel.Commands;
using Financarias.Application.MarketData.Fuel.DTOs.Results;
using Financarias.Application.MarketData.Fuel.Import;
using Financarias.Application.MarketData.Fuel.Queries;
using Financarias.Application.MarketData.Fuel.UseCases;
using Financarias.Application.MarketData.Indexes.Commands;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Application.MarketData.Indexes.UseCases;
using Financarias.Application.MarketData.Indicators.Commands;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Application.MarketData.Indicators.UseCases;
using Financarias.Application.MarketData.Inflation.Commands;
using Financarias.Application.MarketData.IndexComposition.Commands;
using Financarias.Application.MarketData.IndexComposition.Import;
using Financarias.Application.MarketData.IndexComposition.UseCases;
using Financarias.Application.MarketData.Issuers.Commands;
using Financarias.Application.MarketData.Issuers.Import;
using Financarias.Application.MarketData.Issuers.UseCases;
using Financarias.Application.MarketData.Inflation.UseCases;
using Financarias.Application.MarketData.SearchInterest.Commands;
using Financarias.Application.MarketData.SearchInterest.Import;
using Financarias.Application.MarketData.SearchInterest.UseCases;
using Financarias.Application.MarketData.Stocks.Commands;
using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Application.MarketData.Stocks.Queries;
using Financarias.Application.MarketData.Stocks.UseCases;
using Financarias.Application.News;
using Financarias.Application.News.Queries;
using Financarias.Application.News.UseCases;
using Financarias.Domain.Identity;
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

        services.AddScoped<IImportDailyQuotesUseCase, ImportDailyQuotesUseCase>();
        services
            .AddScoped<ICommandHandler<ImportDailyQuotesCommand, DailyQuoteImportResult>,
                ImportDailyQuotesCommandHandler>();

        services.AddScoped<IImportIndexQuotesUseCase, ImportIndexQuotesUseCase>();
        services
            .AddScoped<ICommandHandler<ImportIndexQuotesCommand, IndexQuoteImportResult>,
                ImportIndexQuotesCommandHandler>();

        services.AddScoped<IImportIndicatorValuesUseCase, ImportIndicatorValuesUseCase>();
        services
            .AddScoped<ICommandHandler<ImportIndicatorValuesCommand, IndicatorValueImportResult>,
                ImportIndicatorValuesCommandHandler>();

        services.AddScoped<IRebuildInflationLevelsUseCase, RebuildInflationLevelsUseCase>();
        services
            .AddScoped<ICommandHandler<RebuildInflationLevelsCommand, InflationLevelsRebuildResult>,
                RebuildInflationLevelsCommandHandler>();

        services.AddScoped<IImportIndexCompositionUseCase, ImportIndexCompositionUseCase>();
        services
            .AddScoped<ICommandHandler<ImportIndexCompositionFileCommand, IndexCompositionFileImportResult>,
                ImportIndexCompositionFileCommandHandler>();

        services.AddScoped<IImportIssuersUseCase, ImportIssuersUseCase>();
        services
            .AddScoped<ICommandHandler<ImportIssuersCommand, IssuerImportResult>, ImportIssuersCommandHandler>();

        services.AddScoped<IImportSearchInterestUseCase, ImportSearchInterestUseCase>();
        services
            .AddScoped<ICommandHandler<ImportSearchInterestCommand, SearchInterestImportResult>,
                ImportSearchInterestCommandHandler>();

        services.AddScoped<IIssuerReads, IssuerReads>();
        services.AddScoped<IImportCorporateEventsUseCase, ImportCorporateEventsUseCase>();
        services
            .AddScoped<ICommandHandler<ImportCorporateEventsCommand, CorporateEventImportResult>,
                ImportCorporateEventsCommandHandler>();

        services.AddScoped<IFindEthanolGasolineParityUseCase, FindEthanolGasolineParityUseCase>();
        services
            .AddScoped<IQueryHandler<FindEthanolGasolineParityQuery, IQueryable<EthanolGasolineParityResult>>,
                FindEthanolGasolineParityQueryHandler>();

        services.AddSingleton<UserMapper>();
        services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
        services.AddScoped<ICommandHandler<CreateUserCommand, User>, CreateUserCommandHandler>();
        services.AddScoped<IActivateUserUseCase, ActivateUserUseCase>();
        services.AddScoped<ICommandHandler<ActivateUserCommand, User>, ActivateUserCommandHandler>();
        services.AddScoped<IDeactivateUserUseCase, DeactivateUserUseCase>();
        services.AddScoped<ICommandHandler<DeactivateUserCommand, User>, DeactivateUserCommandHandler>();
        services.AddScoped<ILoginUseCase, LoginUseCase>();
        services.AddScoped<ICommandHandler<LoginCommand, IssuedAccessToken>, LoginCommandHandler>();
        services.AddScoped<IChangePasswordUseCase, ChangePasswordUseCase>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand, User>, ChangePasswordCommandHandler>();
        services.AddScoped<IUserReads, UserReads>();

        services.AddScoped<IFuelReads, FuelReads>();

        return services;
    }
}