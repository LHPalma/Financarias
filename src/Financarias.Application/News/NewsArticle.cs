namespace Financarias.Application.News;

public sealed record NewsArticle(
    string? Title,
    string? Link,
    string? Summary,
    string? SummaryHtml,
    string? Section,
    IReadOnlyList<string> Tags,
    DateTimeOffset? PublishedAt
);