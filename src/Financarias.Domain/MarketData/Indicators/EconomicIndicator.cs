namespace Financarias.Domain.MarketData.Indicators;

/// <summary>Indicador econômico do Banco Central cujo valor é guardado por data de referência.</summary>
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
}
