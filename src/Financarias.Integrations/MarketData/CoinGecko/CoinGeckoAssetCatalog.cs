using Financarias.Domain.MarketData.Cryptos;

namespace Financarias.Integrations.MarketData.CoinGecko;

public static class CoinGeckoAssetCatalog
{
    private static readonly IReadOnlyDictionary<CryptoAsset, string> SlugByAsset =
        new Dictionary<CryptoAsset, string>
        {
            [CryptoAsset.Bitcoin] = "bitcoin",
            [CryptoAsset.Ethereum] = "ethereum",
            [CryptoAsset.Tether] = "tether",
            [CryptoAsset.BinanceCoin] = "binancecoin",
            [CryptoAsset.Solana] = "solana",
            [CryptoAsset.Xrp] = "ripple",
            [CryptoAsset.Cardano] = "cardano",
            [CryptoAsset.Dogecoin] = "dogecoin",
        };

    private static readonly IReadOnlyDictionary<string, CryptoAsset> AssetBySlug =
        SlugByAsset.ToDictionary(kv => kv.Value, kv => kv.Key);

    public static string ToSlug(CryptoAsset asset) => SlugByAsset[asset];

    public static bool TryFromSlug(string slug, out CryptoAsset asset)
        => AssetBySlug.TryGetValue(slug, out asset);

    // Exposto pro teste de completude: todo CryptoAsset tem slug.
    public static IEnumerable<CryptoAsset> MappedAssets => SlugByAsset.Keys;
}