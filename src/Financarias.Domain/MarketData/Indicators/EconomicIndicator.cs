namespace Financarias.Domain.MarketData.Indicators;

/// <summary>Indicador econômico, do Banco Central ou do mercado externo, cujo valor é guardado por data de referência.</summary>
public enum EconomicIndicator
{
    /// <summary>Dólar americano comercial, cotação de venda, em reais por dólar. Diário, a partir de 28/11/1984. Antes de 01/07/1994 o valor está na moeda da época, sem conversão para o real.</summary>
    Dollar,

    /// <summary>Meta da taxa Selic definida pelo Copom, em % ao ano. Diário, a partir de 05/03/1999.</summary>
    SelicTarget,

    /// <summary>Taxa CDI anualizada na base 252, em % ao ano. Diário, a partir de 06/03/1986.</summary>
    Cdi,

    /// <summary>Variação mensal do IPCA, em %. Mensal, a partir de janeiro de 1980, com a data de referência no primeiro dia do mês.</summary>
    Ipca,

    /// <summary>Petróleo Brent (preço à vista na Europa), em dólares por barril. Diário, a partir de 20/05/1987. Fonte: EIA (U.S. Energy Information Administration), série RBRTE.</summary>
    BrentOil,

    /// <summary>Petróleo WTI (preço à vista em Cushing), em dólares por barril. Diário, a partir de 02/01/1986. Fonte: EIA, série RWTC.</summary>
    WtiOil,

    /// <summary>Minério de ferro (62% Fe, CFR China), em dólares por tonelada. Mensal, a partir de janeiro de 1960, com a data de referência no primeiro dia do mês. Fonte: Banco Mundial (Pink Sheet), coluna "Iron ore, cfr spot".</summary>
    IronOre,

    /// <summary>Índice amplo do dólar ponderado pelo comércio (Nominal Broad Dollar Index). Diário, a partir de 02/01/2006. Não é o DXY da ICE, que é pago: este é o índice do Federal Reserve, que pesa mais moedas. Fonte: Federal Reserve, relatório H.10, série JRXWTFB_N.B.</summary>
    DollarIndex,

    /// <summary>VIX, índice de volatilidade implícita das opções do S&amp;P 500, em pontos. Diário, a partir de 02/01/1990. Fonte: Cboe.</summary>
    Vix,
}
