namespace Financarias.Domain.MarketData.IndexComposition;

/// <summary>Estágio de uma carteira teórica de índice da B3: as três prévias que antecedem a virada e a carteira final que entra em vigor.</summary>
public enum IndexPortfolioKind
{
    /// <summary>Primeira prévia, divulgada cerca de um mês antes da virada.</summary>
    FirstPreview,

    /// <summary>Segunda prévia.</summary>
    SecondPreview,

    /// <summary>Terceira e última prévia, divulgada na última semana antes da virada. Costuma ser igual à carteira final.</summary>
    ThirdPreview,

    /// <summary>Carteira final, a que passa a valer na virada do quadrimestre.</summary>
    Final,
}
