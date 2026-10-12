namespace Financarias.Application.MarketData.SearchInterest.DTOs.Requests;

/// <summary>
/// Grupo de termos de busca e intervalo a importar.
/// </summary>
/// <param name="Terms">De 1 a 5 termos, comparados entre si. O índice de 0 a 100 é relativo ao grupo: 100 é o maior valor entre todos os termos e dias da importação, então mudar um termo ou o intervalo muda a escala dos demais. Maiúsculas e espaços nas pontas são ignorados.</param>
/// <param name="Geo">País em duas letras maiúsculas, como `BR`, ou país e região, como `BR-SP`.</param>
/// <param name="From">Primeiro dia do intervalo.</param>
/// <param name="To">Último dia do intervalo, inclusive. O intervalo tem no máximo 270 dias, que é o limite de série diária da fonte.</param>
public sealed record ImportSearchInterestRequest(
    IReadOnlyList<string> Terms,
    string Geo,
    DateOnly From,
    DateOnly To);
