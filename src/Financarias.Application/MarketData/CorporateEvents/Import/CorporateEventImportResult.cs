namespace Financarias.Application.MarketData.CorporateEvents.Import;

/// <summary>Resultado da importação dos eventos corporativos de um emissor.</summary>
/// <param name="IssuerFound">Se o emissor tinha cotações já importadas, de onde saem os nomes de pregão para consultar a B3. Falso quando não tinha, e nesse caso todos os contadores são zero.</param>
/// <param name="NamesTried">Quantos nomes de pregão distintos do emissor foram consultados.</param>
/// <param name="EventsRead">Eventos publicados pela B3 e entendidos: proventos em dinheiro, desdobramentos, grupamentos e bonificações.</param>
/// <param name="EventsSkipped">Registros da B3 pulados por trazerem um tipo ou um valor que não foi entendido.</param>
/// <param name="EventsCreated">Eventos novos gravados.</param>
/// <param name="EventsRemoved">Eventos gravados antes que a B3 não publica mais, removidos para o conjunto ficar igual ao publicado.</param>
public sealed record CorporateEventImportResult(
    bool IssuerFound,
    int NamesTried,
    int EventsRead,
    int EventsSkipped,
    int EventsCreated,
    int EventsRemoved);
