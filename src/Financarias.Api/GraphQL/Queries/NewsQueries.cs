using Financarias.Application.News;
using Financarias.Application.News.UseCases;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class NewsQueries
{
    /// <summary>
    ///     Últimas notícias financeiras (feed RSS do InfoMoney). Não exige autenticação.
    ///
    ///     Sem filtros devolve todas. Com `sections` e/ou `tags`, devolve as notícias que estiverem em alguma das seções **ou** tiverem alguma das tags (união, não interseção). A comparação ignora maiúsculas e minúsculas.
    /// </summary>
    /// <param name="sections">Seções a incluir.</param>
    /// <param name="tags">Tags a incluir.</param>
    [GraphQLName("latestNews")]
    public Task<IReadOnlyList<NewsArticle>> GetLatestNewsAsync(
        IGetLatestNewsUseCase useCase,
        CancellationToken cancellationToken,
        IReadOnlyList<NewsCategory>? sections = null,
        IReadOnlyList<string>? tags = null) =>
        useCase.ExecuteAsync(sections, tags, cancellationToken);
}
