using Financarias.Domain.MarketData.Cryptos;

namespace Financarias.Application.MarketData.Cryptos.DTOs.Results;

/// <summary>Cotação de um criptoativo.</summary>
/// <param name="Asset">Criptoativo cotado.</param>
/// <param name="Symbol">Símbolo do ativo, como informado pela fonte.</param>
/// <param name="Name">Nome do ativo, como informado pela fonte.</param>
/// <param name="Currency">Moeda em que os valores monetários estão expressos.</param>
/// <param name="Price">Preço atual.</param>
/// <param name="MarketCap">Valor de mercado (capitalização), quando a fonte informa.</param>
/// <param name="Volume">Volume negociado nas últimas 24 horas, quando a fonte informa.</param>
/// <param name="High24h">Máxima das últimas 24 horas.</param>
/// <param name="Low24h">Mínima das últimas 24 horas.</param>
/// <param name="PriceChange24h">Variação absoluta do preço em 24 horas.</param>
/// <param name="PriceChangePercent24h">Variação do preço em 24 horas em percentual (2,5 significa 2,5%).</param>
/// <param name="AsOf">Instante da última atualização informada pela fonte.</param>
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