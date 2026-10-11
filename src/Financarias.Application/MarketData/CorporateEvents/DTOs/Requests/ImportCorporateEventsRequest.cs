namespace Financarias.Application.MarketData.CorporateEvents.DTOs.Requests;

/// <summary>
/// Emissor cujos eventos corporativos serão importados.
/// </summary>
/// <param name="Issuer">Código do emissor, quatro letras ou dígitos em maiúsculas, que é a raiz do ISIN depois do `BR`: `PETR` para a Petrobras, `ITUB` para o Itaú Unibanco. O emissor precisa ter cotações já importadas, porque os nomes de pregão que a B3 espera saem delas.</param>
public sealed record ImportCorporateEventsRequest(string Issuer);
