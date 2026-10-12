using Ardalis.Specification;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.SearchInterest.Commands;
using Financarias.Application.MarketData.SearchInterest.Import;
using Financarias.Domain.MarketData.SearchInterest;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.SearchInterest.Commands;

public class ImportSearchInterestCommandHandlerTests
{
    private static readonly DateOnly From = new(2023, 1, 1);
    private static readonly DateOnly To = new(2023, 1, 31);
    private static readonly IReadOnlyList<string> Terms = ["americanas", "magalu"];

    private readonly ISearchInterestProvider _provider = Substitute.For<ISearchInterestProvider>();
    private readonly IRepository<SearchInterestPoint> _repository = Substitute.For<IRepository<SearchInterestPoint>>();

    [Fact(DisplayName = "Cria os pontos novos com o grupo, o intervalo e o termo normalizado")]
    public async Task Handle_CreatesNewPoints()
    {
        // Arrange
        GivenProvider(Item("Americanas", 12, 100), Item("magalu", 12, 9));
        GivenExisting();
        List<SearchInterestPoint>? added = null;
        await _repository.AddRangeAsync(Arg.Do<IEnumerable<SearchInterestPoint>>(items => added = items.ToList()), Arg.Any<CancellationToken>());

        // Act
        var result = await CreateHandler().HandleAsync(new ImportSearchInterestCommand(Terms, "BR", From, To));

        // Assert
        Assert.Equal(new SearchInterestImportResult(2, 2, 0), result);
        Assert.Equal(["americanas", "magalu"], added!.Select(point => point.Term));
        Assert.All(added, point =>
        {
            Assert.Equal("americanas|magalu", point.ComparisonKey);
            Assert.Equal(From, point.RangeStart);
            Assert.Equal(To, point.RangeEnd);
        });
    }

    [Fact(DisplayName = "Atualiza o ponto cujo valor mudou e não grava o que ficou igual")]
    public async Task Handle_UpdatesOnlyChangedPoints()
    {
        // Arrange
        var changed = SearchInterestPoint.Create("BR", Terms, From, To, "americanas", new DateOnly(2023, 1, 12), 90);
        var same = SearchInterestPoint.Create("BR", Terms, From, To, "magalu", new DateOnly(2023, 1, 12), 9);
        GivenProvider(Item("americanas", 12, 100), Item("magalu", 12, 9));
        GivenExisting(changed, same);

        // Act
        var result = await CreateHandler().HandleAsync(new ImportSearchInterestCommand(Terms, "BR", From, To));

        // Assert
        Assert.Equal(new SearchInterestImportResult(2, 0, 1), result);
        Assert.Equal(100, changed.Value);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddRangeAsync(Arg.Any<IEnumerable<SearchInterestPoint>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Repetir a importação sem mudança não grava nada")]
    public async Task Handle_Twice_ChangesNothing()
    {
        // Arrange
        var existing = SearchInterestPoint.Create("BR", Terms, From, To, "americanas", new DateOnly(2023, 1, 12), 100);
        GivenProvider(Item("americanas", 12, 100));
        GivenExisting(existing);

        // Act
        var result = await CreateHandler().HandleAsync(new ImportSearchInterestCommand(Terms, "BR", From, To));

        // Assert
        Assert.Equal(new SearchInterestImportResult(1, 0, 0), result);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Fonte sem pontos devolve zeros sem consultar o banco")]
    public async Task Handle_WithoutItems_ReturnsZeroes()
    {
        // Arrange
        GivenProvider();

        // Act
        var result = await CreateHandler().HandleAsync(new ImportSearchInterestCommand(Terms, "BR", From, To));

        // Assert
        Assert.Equal(new SearchInterestImportResult(0, 0, 0), result);
        await _repository.DidNotReceive().ListAsync(Arg.Any<ISpecification<SearchInterestPoint>>(), Arg.Any<CancellationToken>());
    }

    private ImportSearchInterestCommandHandler CreateHandler() => new(_provider, _repository);

    private static SearchInterestImportItem Item(string term, int day, int value) => new(term, new DateOnly(2023, 1, day), value);

    private void GivenProvider(params SearchInterestImportItem[] items) =>
        _provider.GetAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(items);

    private void GivenExisting(params SearchInterestPoint[] points) =>
        _repository.ListAsync(Arg.Any<ISpecification<SearchInterestPoint>>(), Arg.Any<CancellationToken>())
            .Returns(points.ToList());
}
