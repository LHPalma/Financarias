using Financarias.Domain.MarketData.Cryptos;

namespace Financarias.Application.MarketData.Cryptos.DTOs.Results;

public sealed record CryptoQuoteResult(
    CryptoAsset Asset,
    string? Symbol,
    string? Name,
    QuoteCurrency Currency,
    decimal Price,
    decimal? MarketCap,
    decimal? Volume,
    decimal? High24h,
    decimal? Low24h,
    decimal? PriceChange24h,
    decimal? PriceChangePercent24h,
    DateTimeOffset AsOf
);