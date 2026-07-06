using Financarias.Application.Common.Messaging;

namespace Financarias.Application.News.Queries;

public sealed record GetLatestNewsQuery(
    IReadOnlyList<NewsCategory> Sections,
    IReadOnlyList<string> Tags
) : IQuery<IReadOnlyList<NewsArticle>>;