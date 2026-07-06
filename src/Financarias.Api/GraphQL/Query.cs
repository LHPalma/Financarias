using Financarias.Application.Addresses;
using Financarias.Application.Addresses.UseCases;
using Financarias.Application.Analytics;
using Financarias.Application.Analytics.DTOs.Requests;
using Financarias.Application.Analytics.DTOs.Results;
using Financarias.Application.Analytics.UseCases;
using Financarias.Application.Holidays.UseCases;
using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Application.MarketData.Cryptos.UseCases;
using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Application.MarketData.Stocks.UseCases;
using Financarias.Application.News;
using Financarias.Application.News.UseCases;
using Financarias.Domain.Holidays.Models;
using Financarias.Domain.MarketData.Cryptos;

namespace Financarias.Api.GraphQL;

public class Query
{
    [GraphQLName("addressLookup")]
    public Task<AddressLookupResult?> AddressLookupAsync(
        string cep,
        IFindAddressByCepUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(cep, cancellationToken);

    [GraphQLName("simulateNtnbScenario")]
    public Task<ScenarioResult> SimulateNtnbScenarioAsync(
        decimal vna,
        decimal buyYield,
        decimal sellYield,
        int businessDays,
        ISimulateScenarioUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(vna, buyYield, sellYield, businessDays, cancellationToken);

    [GraphQLName("businessDayCount")]
    public Task<int> BusinessDayCountAsync(
        DateOnly start,
        DateOnly end,
        CountryCode countryCode,
        ICountBusinessDaysUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(start, end, countryCode, cancellationToken);

    [GraphQLName("calculateNtnbPrice")]
    public Task<NtnbPriceResult> CalculateNtnbPriceAsync(
        CalculateNtnbPriceRequest input,
        ICalculateNtnbPriceUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);

    [GraphQLName("stockQuote")]
    public Task<StockQuoteResult?> GetStockQuoteAsync(
        string ticker,
        IGetStockQuoteUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(ticker, cancellationToken);

    #region Cryptocurrency

    [GraphQLName("cryptoQuotes")]
    public Task<IReadOnlyList<CryptoQuoteResult>> GetCryptoQuotesAsync(
        IReadOnlyList<CryptoAsset> assets,
        QuoteCurrency currency,
        IGetCryptoQuotesUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(assets, currency, cancellationToken);

    [GraphQLName("availableCryptoAssets")]
    public Task<CryptoAsset[]> GetAvailableAssetsAsync() => Task.FromResult(Enum.GetValues<CryptoAsset>());

    [GraphQLName("availableQuoteCurrencies")]
    public Task<QuoteCurrency[]> GetAvailableQuotesCurrenciesAsync() =>
        Task.FromResult(Enum.GetValues<QuoteCurrency>());

    #endregion

    #region News

    [GraphQLName("latestNews")]
    public Task<IReadOnlyList<NewsArticle>> GetLatestNewsAsync(
        IGetLatestNewsUseCase useCase,
        CancellationToken cancellationToken,
        IReadOnlyList<NewsCategory>? sections = null,
        IReadOnlyList<string>? tags = null) =>
        useCase.ExecuteAsync(sections, tags, cancellationToken);

    #endregion
}