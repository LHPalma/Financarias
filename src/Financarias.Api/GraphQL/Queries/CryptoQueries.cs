using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Application.MarketData.Cryptos.UseCases;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Cryptos;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class CryptoQueries
{
    /// <summary>
    ///     Cotações de criptoativos (fonte: CoinGecko) na moeda escolhida. Ativos que a fonte não devolver, ou devolver sem preço, ficam de fora da lista. Não exige autenticação.
    /// </summary>
    /// <param name="assets">Ativos a cotar.</param>
    /// <param name="currency">Moeda em que as cotações são expressas.</param>
    [GraphQLName("cryptoQuotes")]
    public Task<IReadOnlyList<CryptoQuoteResult>> GetCryptoQuotesAsync(
        IReadOnlyList<CryptoAsset> assets,
        QuoteCurrency currency,
        IGetCryptoQuotesUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(assets, currency, cancellationToken);

    /// <summary>
    ///     Lista os criptoativos que `cryptoQuotes` aceita. Não exige autenticação.
    /// </summary>
    [GraphQLName("availableCryptoAssets")]
    public Task<CryptoAsset[]> GetAvailableAssetsAsync() => Task.FromResult(Enum.GetValues<CryptoAsset>());

    /// <summary>
    ///     Lista as moedas de cotação que `cryptoQuotes` aceita. Não exige autenticação.
    /// </summary>
    [GraphQLName("availableQuoteCurrencies")]
    public Task<QuoteCurrency[]> GetAvailableQuotesCurrenciesAsync() =>
        Task.FromResult(Enum.GetValues<QuoteCurrency>());
}
