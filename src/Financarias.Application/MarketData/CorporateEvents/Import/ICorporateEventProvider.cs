namespace Financarias.Application.MarketData.CorporateEvents.Import;

/// <summary>
/// Porta de origem dos eventos corporativos de um emissor: obtém, de um provedor externo (ex.: empresas
/// listadas da B3), os proventos publicados sob cada nome de pregão e os eventos sobre as ações do emissor.
/// Emissor ou nome sem dados devolve lista vazia; falha de transporte propaga.
/// </summary>
public interface ICorporateEventProvider
{
    Task<CorporateEventFetch> GetAsync(
        string issuerCode,
        IReadOnlyCollection<string> tradingNames,
        CancellationToken cancellationToken = default);
}

/// <summary>Eventos lidos de um emissor e quantos registros do provedor foram pulados por não serem entendidos.</summary>
public sealed record CorporateEventFetch(IReadOnlyList<CorporateEventImportItem> Items, int Skipped);
