namespace Financarias.Application.News;

/// <summary>
///     Porta de saída para o feed de notícias financeiras. Retorna as últimas notícias publicadas.
/// </summary>
public interface INewsFeedGateway
{
    Task<IReadOnlyList<NewsArticle>> GetLatestAsync(CancellationToken cancellationToken = default);
}