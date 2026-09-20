namespace Financarias.Application.MarketData.Stocks.DTOs.Results;

/// <summary>Cotação de uma ação na bolsa.</summary>
/// <param name="Symbol">Ticker da ação, como devolvido pela fonte.</param>
/// <param name="Name">Nome curto da empresa.</param>
/// <param name="Currency">Código da moeda da cotação, como BRL. Quando a fonte não informa, é BRL.</param>
/// <param name="Price">Preço atual.</param>
/// <param name="Change">Variação absoluta do preço no dia.</param>
/// <param name="ChangePercent">Variação do preço no dia em percentual (1,5 significa 1,5%).</param>
/// <param name="Open">Preço de abertura do dia.</param>
/// <param name="DayHigh">Máxima do dia.</param>
/// <param name="DayLow">Mínima do dia.</param>
/// <param name="PreviousClose">Preço de fechamento do pregão anterior.</param>
/// <param name="Volume">Volume negociado no dia, em número de ações.</param>
/// <param name="AsOf">Instante da cotação. Quando a fonte não informa, é o instante da consulta.</param>
public sealed record StockQuoteResult(
    string Symbol,
    string? Name,
    string Currency,
    decimal Price,
    decimal? Change,
    decimal? ChangePercent,
    decimal? Open,
    decimal? DayHigh,
    decimal? DayLow,
    decimal? PreviousClose,
    long? Volume,
    DateTimeOffset AsOf
);