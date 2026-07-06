namespace Financarias.Application.News.UseCases;

/// <summary>
///     Caso de uso de consulta das últimas notícias financeiras.
/// </summary>
public interface IGetLatestNewsUseCase
{
    Task<IReadOnlyList<NewsArticle>> ExecuteAsync(
        IReadOnlyList<NewsCategory>? sections = null,
        IReadOnlyList<string>? tags = null,
        CancellationToken cancellationToken = default);
}