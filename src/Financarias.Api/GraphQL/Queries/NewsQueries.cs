using Financarias.Application.News;
using Financarias.Application.News.UseCases;

namespace Financarias.Api.GraphQL.Queries;

[ExtendObjectType(OperationTypeNames.Query)]
public class NewsQueries
{
    [GraphQLName("latestNews")]
    public Task<IReadOnlyList<NewsArticle>> GetLatestNewsAsync(
        IGetLatestNewsUseCase useCase,
        CancellationToken cancellationToken,
        IReadOnlyList<NewsCategory>? sections = null,
        IReadOnlyList<string>? tags = null) =>
        useCase.ExecuteAsync(sections, tags, cancellationToken);
}
