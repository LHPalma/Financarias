using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.SearchInterest.Commands;
using Financarias.Application.MarketData.SearchInterest.DTOs.Requests;
using Financarias.Application.MarketData.SearchInterest.Import;
using Financarias.Application.MarketData.SearchInterest.UseCases;
using Financarias.Domain.Common.Exceptions;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.SearchInterest.UseCases;

public class ImportSearchInterestUseCaseTests
{
    private static readonly DateOnly From = new(2023, 1, 1);
    private static readonly DateOnly To = new(2023, 1, 31);

    private readonly ICommandHandler<ImportSearchInterestCommand, SearchInterestImportResult> _handler =
        Substitute.For<ICommandHandler<ImportSearchInterestCommand, SearchInterestImportResult>>();

    [Fact(DisplayName = "Normaliza os termos, tira repetidos e entrega o comando ao handler")]
    public async Task Execute_NormalizesTheTerms()
    {
        // Arrange
        var expected = new SearchInterestImportResult(1, 1, 0);
        _handler.HandleAsync(Arg.Any<ImportSearchInterestCommand>(), Arg.Any<CancellationToken>()).Returns(expected);

        // Act
        var result = await new ImportSearchInterestUseCase(_handler)
            .ExecuteAsync(new ImportSearchInterestRequest([" Americanas", "americanas ", "Magalu"], "BR", From, To));

        // Assert
        Assert.Equal(expected, result);
        await _handler.Received(1).HandleAsync(
            Arg.Is<ImportSearchInterestCommand>(command => command.Terms.SequenceEqual(new[] { "americanas", "magalu" }) && command.Geo == "BR"),
            Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "Recusa grupo vazio, com mais de cinco termos, país inválido e intervalo inválido, sem chamar o handler")]
    [InlineData(0, "BR", 30)]
    [InlineData(6, "BR", 30)]
    [InlineData(2, "br", 30)]
    [InlineData(2, "BR", 270)]
    [InlineData(2, "BR", -1)]
    public async Task Execute_WithInvalidRequest_ThrowsDomainError(int termCount, string geo, int rangeDays)
    {
        // Arrange
        var terms = Enumerable.Range(1, termCount).Select(i => $"termo {i}").ToList();
        var request = new ImportSearchInterestRequest(terms, geo, From, From.AddDays(rangeDays));

        // Act
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => new ImportSearchInterestUseCase(_handler).ExecuteAsync(request));

        // Assert
        Assert.Equal("searchinterest.point.invalid", exception.Code);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ImportSearchInterestCommand>(), Arg.Any<CancellationToken>());
    }
}
