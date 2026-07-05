using Financarias.Domain.MarketData.Cryptos;
using Financarias.Integrations.MarketData.CoinGecko;

namespace Financarias.Integrations.UnitTests.MarketData.CoinGecko;

public class CoinGeckoAssetCatalogTests
{
    [Fact(DisplayName = "Todo CryptoAsset tem slug mapeado (guard de completude)")]
    public void Catalog_CoversEveryCryptoAsset()
    {
        // Act
        var missing = Enum.GetValues<CryptoAsset>().Except(CoinGeckoAssetCatalog.MappedAssets).ToList();

        // Assert
        Assert.Empty(missing);
    }

    [Theory(DisplayName = "ToSlug e TryFromSlug fazem round-trip (inclui Xrp -> ripple)")]
    [InlineData(CryptoAsset.Bitcoin, "bitcoin")]
    [InlineData(CryptoAsset.Xrp, "ripple")]
    [InlineData(CryptoAsset.BinanceCoin, "binancecoin")]
    public void ToSlug_And_TryFromSlug_RoundTrip(CryptoAsset asset, string slug)
    {
        // Act & Assert
        Assert.Equal(slug, CoinGeckoAssetCatalog.ToSlug(asset));
        Assert.True(CoinGeckoAssetCatalog.TryFromSlug(slug, out var back));
        Assert.Equal(asset, back);
    }

    [Fact(DisplayName = "TryFromSlug retorna false para slug desconhecido")]
    public void TryFromSlug_WithUnknownSlug_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(CoinGeckoAssetCatalog.TryFromSlug("not-a-coin", out _));
    }
}
