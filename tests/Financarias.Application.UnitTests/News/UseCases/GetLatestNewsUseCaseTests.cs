using Financarias.Application.Common.Messaging;
using Financarias.Application.News;
using Financarias.Application.News.Queries;
using Financarias.Application.News.UseCases;
using NSubstitute;

namespace Financarias.Application.UnitTests.News.UseCases;

public class GetLatestNewsUseCaseTests
{
    private readonly IQueryHandler<GetLatestNewsQuery, IReadOnlyList<NewsArticle>> _handler =
        Substitute.For<IQueryHandler<GetLatestNewsQuery, IReadOnlyList<NewsArticle>>>();

    [Fact(DisplayName = "Monta a query com as seções/tags e delega ao handler")]
    public async Task Execute_DelegatesToHandlerWithSectionsAndTags()
    {
        // Arrange
        var expected = new List<NewsArticle>();
        _handler.HandleAsync(Arg.Any<GetLatestNewsQuery>(), Arg.Any<CancellationToken>()).Returns(expected);
        var useCase = new GetLatestNewsUseCase(_handler);

        // Act
        var result = await useCase.ExecuteAsync([NewsCategory.Mercados], ["Petróleo"]);

        // Assert
        Assert.Same(expected, result);
        await _handler.Received(1).HandleAsync(
            Arg.Is<GetLatestNewsQuery>(q =>
                q.Sections.Contains(NewsCategory.Mercados) && q.Tags.Contains("Petróleo")),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Sem argumentos passa listas vazias ao handler")]
    public async Task Execute_WithoutArguments_PassesEmptyLists()
    {
        // Arrange
        _handler.HandleAsync(Arg.Any<GetLatestNewsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new List<NewsArticle>());
        var useCase = new GetLatestNewsUseCase(_handler);

        // Act
        await useCase.ExecuteAsync();

        // Assert
        await _handler.Received(1).HandleAsync(
            Arg.Is<GetLatestNewsQuery>(q => q.Sections.Count == 0 && q.Tags.Count == 0),
            Arg.Any<CancellationToken>());
    }
}
