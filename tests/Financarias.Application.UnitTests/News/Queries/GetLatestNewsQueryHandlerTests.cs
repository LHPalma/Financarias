using Financarias.Application.News;
using Financarias.Application.News.Queries;
using NSubstitute;

namespace Financarias.Application.UnitTests.News.Queries;

public class GetLatestNewsQueryHandlerTests
{
    private readonly INewsFeedGateway _gateway = Substitute.For<INewsFeedGateway>();

    private static NewsArticle Article(string title, string? section, params string[] tags) =>
        new(title, null, null, null, section, tags, null);

    private void FeedReturns(params NewsArticle[] articles) =>
        _gateway.GetLatestAsync(Arg.Any<CancellationToken>()).Returns(articles);

    [Fact(DisplayName = "Sem filtro retorna todas as notícias do gateway")]
    public async Task Handle_WithoutFilter_ReturnsAll()
    {
        // Arrange
        FeedReturns(Article("a", "Mercados"), Article("b", "Esportes"));
        var handler = new GetLatestNewsQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(new GetLatestNewsQuery([], []));

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact(DisplayName = "Filtro por seção casa contra a seção do artigo, não contra as tags")]
    public async Task Handle_WithSection_MatchesArticleSectionOnly()
    {
        // Arrange
        FeedReturns(
            Article("mercado", "Mercados", "Ibovespa"),
            Article("esporte com tag mercados", "Esportes", "Mercados"));
        var handler = new GetLatestNewsQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(new GetLatestNewsQuery([NewsCategory.Mercados], []));

        // Assert
        Assert.Equal("mercado", Assert.Single(result).Title);
    }

    [Fact(DisplayName = "Filtro por tag casa contra as tags do artigo, ignorando a caixa")]
    public async Task Handle_WithTag_MatchesArticleTagsIgnoringCase()
    {
        // Arrange
        FeedReturns(
            Article("com petroleo", "Mercados", "Opep+", "Petróleo"),
            Article("sem", "Mercados", "Ibovespa"));
        var handler = new GetLatestNewsQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(new GetLatestNewsQuery([], ["petróleo"]));

        // Assert
        Assert.Equal("com petroleo", Assert.Single(result).Title);
    }

    [Fact(DisplayName = "Seção e tag combinam por OR")]
    public async Task Handle_WithSectionAndTag_CombinesWithOr()
    {
        // Arrange
        FeedReturns(
            Article("mundo com copa", "Mundo", "Copa"),
            Article("mercado qualquer", "Mercados", "Ibovespa"),
            Article("esporte sem match", "Esportes", "Futebol"));
        var handler = new GetLatestNewsQueryHandler(_gateway);

        // Act
        var result = await handler.HandleAsync(new GetLatestNewsQuery([NewsCategory.Mercados], ["Copa"]));

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, article => article.Title == "mundo com copa");
        Assert.Contains(result, article => article.Title == "mercado qualquer");
    }
}
