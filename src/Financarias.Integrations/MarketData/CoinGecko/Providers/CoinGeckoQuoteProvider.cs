using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Application.MarketData.Cryptos.Gateways;
using Financarias.Domain.MarketData.Cryptos;
using Financarias.Integrations.MarketData.CoinGecko.Clients;
using Microsoft.Extensions.Logging;

namespace Financarias.Integrations.MarketData.CoinGecko.Providers;

public sealed class CoinGeckoQuoteProvider(
    ICoinGeckoClient client,
    ILogger<CoinGeckoQuoteProvider> logger
) : ICryptoQuoteGateway
{
    public async Task<IReadOnlyList<CryptoQuoteResult>> FindQuotesAsync(
        IReadOnlyList<CryptoAsset> assets,
        QuoteCurrency currency,
        CancellationToken cancellationToken = default)
    {
        if (assets.Count == 0)
        {
            return [];
        }

        var ids = string.Join(
            ',',
            assets.Select(CoinGeckoAssetCatalog.ToSlug)
        );

        var vsCurrency = currency.ToString().ToLowerInvariant();

        var response = await client.GetMarketsAsync(vsCurrency, ids, cancellationToken);

        var quotes = new List<CryptoQuoteResult>(response.Count);

        foreach (var data in response)
        {
            if (data.Id is null || !CoinGeckoAssetCatalog.TryFromSlug(data.Id, out var asset))
            {
                logger.LogWarning("CoinGecko devolveu id desconhecido: {Id}", data.Id);
                continue;
            }

            if (data.CurrentPrice is null)
            {
                logger.LogWarning("CoinGecko sem preço para {Asset}", asset);
                continue;
            }

            quotes.Add(new CryptoQuoteResult(
                Asset: asset,
                Symbol: data.Symbol,
                Name: data.Name,
                Currency: currency,
                Price: data.CurrentPrice.Value,
                MarketCap: data.MarketCap,
                Volume: data.TotalVolume,
                High24h: data.High24h,
                Low24h: data.Low24h,
                PriceChange24h: data.PriceChange24h,
                PriceChangePercent24h: data.PriceChangePercentage24h,
                AsOf: data.LastUpdated ?? DateTimeOffset.UtcNow));
        }

        return quotes;
    }
}