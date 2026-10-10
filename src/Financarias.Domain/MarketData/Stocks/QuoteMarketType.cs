namespace Financarias.Domain.MarketData.Stocks;

/// <summary>Mercado em que o papel foi negociado, como classificado pela B3 no COTAHIST.</summary>
public enum QuoteMarketType
{
    /// <summary>Mercado à vista, em lote padrão (TPMERC 010).</summary>
    Cash,

    /// <summary>Mercado fracionário, em unidades abaixo do lote padrão (TPMERC 020).</summary>
    Fractional,
}
