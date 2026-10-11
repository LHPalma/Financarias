namespace Financarias.Application.MarketData.CorporateEvents.Queries;

public interface IIssuerReads
{
    /// <summary>
    /// Nomes de pregão distintos com que o emissor aparece nas cotações já importadas, achados pelo prefixo
    /// do ISIN (<c>BR</c> + código do emissor). Vazio quando o emissor não tem cotação.
    /// </summary>
    Task<IReadOnlyList<string>> TradingNamesAsync(string issuerCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Códigos dos emissores que aparecem nas cotações já importadas, achados pelo ISIN
    /// (<c>BR</c> + quatro caracteres do emissor), em ordem alfabética.
    /// </summary>
    Task<IReadOnlyList<string>> IssuerCodesAsync(CancellationToken cancellationToken = default);
}
