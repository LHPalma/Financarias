using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Application.MarketData.Cryptos.UseCases;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Cryptos;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class CryptoQueries
{
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
}
