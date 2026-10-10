namespace Financarias.Application.MarketData.Stocks.Import;

/// <summary>Resultado da importação de cotações diárias do COTAHIST da B3.</summary>
/// <param name="FileFound">Se o arquivo do período existia na B3. Falso em fim de semana, feriado ou data futura, e nesse caso todos os contadores são zero.</param>
/// <param name="RowsRead">Cotações de mercado à vista ou fracionário lidas do arquivo e processadas.</param>
/// <param name="RowsSkipped">Linhas desses mercados que foram puladas por estarem malformadas ou com valores inválidos.</param>
/// <param name="QuotesCreated">Cotações novas inseridas.</param>
/// <param name="QuotesUpdated">Cotações já existentes (mesmo papel, pregão e mercado) cujos valores mudaram e foram atualizados.</param>
public sealed record DailyQuoteImportResult(
    bool FileFound,
    int RowsRead,
    int RowsSkipped,
    int QuotesCreated,
    int QuotesUpdated);
