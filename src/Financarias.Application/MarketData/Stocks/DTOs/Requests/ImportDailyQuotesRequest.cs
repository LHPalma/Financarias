namespace Financarias.Application.MarketData.Stocks.DTOs.Requests;

/// <summary>
/// Período do COTAHIST a importar. Informe exatamente um dos campos: o servidor recusa a entrada com mais de um ou com nenhum.
/// </summary>
/// <param name="Day">Um pregão específico, como 2026-10-09. Fim de semana, feriado e data futura não têm arquivo na B3, e o resultado vem com `fileFound` falso.</param>
/// <param name="Month">Um mês inteiro. Arquivos mensais têm dezenas de milhares de cotações.</param>
/// <param name="Year">Um ano inteiro, a partir de 1986. O arquivo anual tem cerca de 90 MB e leva minutos para importar.</param>
public sealed record ImportDailyQuotesRequest(
    DateOnly? Day,
    QuoteMonthRequest? Month,
    int? Year);
