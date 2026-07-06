using Financarias.Application.Common.Messaging;

namespace Financarias.Application.News.Queries;

public class GetLatestNewsQueryHandler(
    INewsFeedGateway gateway
) : IQueryHandler<GetLatestNewsQuery, IReadOnlyList<NewsArticle>>
{
    public async Task<IReadOnlyList<NewsArticle>> HandleAsync(
        GetLatestNewsQuery query,
        CancellationToken cancellationToken = default)
    {
        var articles = await gateway.GetLatestAsync(cancellationToken);

        if (query.Sections.Count == 0 && query.Tags.Count == 0)
        {
            return articles;
        }

        var sections = query.Sections
            .Select(section => section.ToFeedLabel())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var tags = query.Tags.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return articles
            .Where(article =>
                (article.Section is not null && sections.Contains(article.Section))
                || article.Tags.Any(tags.Contains))
            .ToList();
    }
}