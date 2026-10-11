using Financarias.Application.MarketData.CorporateEvents.DTOs.Requests;
using Financarias.Application.MarketData.CorporateEvents.Import;

namespace Financarias.Application.MarketData.CorporateEvents.UseCases;

public interface IImportCorporateEventsUseCase
{
    /// <summary>
    /// Importa da B3 os eventos corporativos de um emissor e deixa o conjunto gravado igual ao publicado;
    /// lança quando o código do emissor não é válido. Emissor sem cotações importadas devolve resultado zerado.
    /// </summary>
    Task<CorporateEventImportResult> ExecuteAsync(
        ImportCorporateEventsRequest request,
        CancellationToken cancellationToken = default);
}
