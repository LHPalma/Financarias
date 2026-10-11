namespace Financarias.Application.Holidays.Import;


/// <summary>
/// Porta de origem dos feriados para importação: devolve a lista já parseada de um provedor
/// externo (ex.: planilha da Anbima) como itens neutros de domínio. O adapter engole falhas
/// defensivamente e retorna lista vazia em caso de erro. Pode haver mais de uma fonte registrada,
/// cada uma cobrindo um período; o caso de uso junta todas e grava o que ainda não existe.
/// </summary>
public interface IHolidayProvider
{
    Task<IReadOnlyList<HolidayImportItem>> FetchHolidaysAsync(CancellationToken cancellationToken = default);
}