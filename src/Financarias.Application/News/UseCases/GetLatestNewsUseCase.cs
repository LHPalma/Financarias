using Financarias.Application.Common.Messaging;
using Financarias.Application.News.Queries;

namespace Financarias.Application.News.UseCases;

public class GetLatestNewsUseCase(
    IQueryHandler<GetLatestNewsQuery, IReadOnlyList<NewsArticle>> handler
) : IGetLatestNewsUseCase
{
    public Task<IReadOnlyList<NewsArticle>> ExecuteAsync(
        IReadOnlyList<NewsCategory>? sections = null,
        IReadOnlyList<string>? tags = null,
        CancellationToken cancellationToken = default)
        => handler.HandleAsync(new GetLatestNewsQuery(sections ?? [], tags ?? []), cancellationToken);
}