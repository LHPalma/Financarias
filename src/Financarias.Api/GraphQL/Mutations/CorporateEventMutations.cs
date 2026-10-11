using Financarias.Application.MarketData.CorporateEvents.DTOs.Requests;
using Financarias.Application.MarketData.CorporateEvents.Import;
using Financarias.Application.MarketData.CorporateEvents.UseCases;

namespace Financarias.Api.GraphQL.Mutations;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class CorporateEventMutations
{
    /// <summary>
    ///     Importa da B3 os eventos corporativos de um emissor: dividendos, juros sobre capital próprio e rendimentos, e também desdobramentos, grupamentos e bonificações. Deixa o conjunto gravado igual ao que a B3 publica: o que já existe e não mudou fica como está, o que é novo entra, e o que a B3 deixou de publicar sai. Parcelas idênticas contam uma a uma e não são tratadas como duplicata.
    ///
    ///     Os nomes de pregão que a B3 espera saem das cotações já importadas, então o emissor precisa ter cotações. Uma empresa que saiu da bolsa pode ter eventos incompletos ou nenhum, porque a B3 responde de forma irregular por ela; quando a B3 não devolve evento algum, o que já estava gravado é mantido.
    ///
    ///     É uma operação de manutenção, com algumas chamadas à B3 por emissor, e hoje não exige autenticação: qualquer chamador pode dispará-la.
    ///
    ///     Erro (`extensions.code`): `corporate.event.invalid`, quando o código do emissor não são quatro letras ou dígitos em maiúsculas. Emissor sem cotações não é erro e devolve `issuerFound` falso.
    /// </summary>
    /// <param name="input">Emissor a importar.</param>
    [GraphQLName("importCorporateEvents")]
    public Task<CorporateEventImportResult> ImportCorporateEventsAsync(
        ImportCorporateEventsRequest input,
        IImportCorporateEventsUseCase useCase,
        CancellationToken cancellationToken) =>
        useCase.ExecuteAsync(input, cancellationToken);
}
