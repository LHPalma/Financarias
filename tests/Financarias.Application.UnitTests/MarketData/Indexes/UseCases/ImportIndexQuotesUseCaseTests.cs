using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Indexes.Commands;
using Financarias.Application.MarketData.Indexes.DTOs.Requests;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Application.MarketData.Indexes.UseCases;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData.Indexes;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Indexes.UseCases;

public class ImportIndexQuotesUseCaseTests
{
    private static readonly IndexQuoteImportResult Result = new(246, 200, 3);

    private readonly ICommandHandler<ImportIndexQuotesCommand, IndexQuoteImportResult> _handler =
        Substitute.For<ICommandHandler<ImportIndexQuotesCommand, IndexQuoteImportResult>>();

    private readonly ImportIndexQuotesUseCase _useCase;

    public ImportIndexQuotesUseCaseTests()
    {
        _handler.HandleAsync(Arg.Any<ImportIndexQuotesCommand>(), Arg.Any<CancellationToken>()).Returns(Result);
        _useCase = new ImportIndexQuotesUseCase(_handler);
    }

    [Fact(DisplayName = "Um ano válido vira o comando e o resultado do handler é devolvido")]
    public async Task Execute_WithValidYear_ForwardsTheCommand()
    {
        // Act
        var result = await _useCase.ExecuteAsync(new ImportIndexQuotesRequest(MarketIndex.Ibovespa, 2018));

        // Assert
        Assert.Same(Result, result);
        await _handler.Received(1).HandleAsync(
            Arg.Is<ImportIndexQuotesCommand>(command => command == new ImportIndexQuotesCommand(MarketIndex.Ibovespa, 2018)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "O primeiro ano publicado (1968) é aceito")]
    public async Task Execute_WithFirstYear_IsAccepted()
    {
        // Act
        await _useCase.ExecuteAsync(new ImportIndexQuotesRequest(MarketIndex.Ibovespa, 1968));

        // Assert
        await _handler.Received(1).HandleAsync(Arg.Any<ImportIndexQuotesCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "Ano anterior a 1968 lança erro de domínio sem chamar o handler")]
    [InlineData(1967)]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Execute_WithYearBeforeFirst_ThrowsWithoutCallingTheHandler(int year)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _useCase.ExecuteAsync(new ImportIndexQuotesRequest(MarketIndex.Ibovespa, year)));

        Assert.Equal("index.year.invalid", exception.Code);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ImportIndexQuotesCommand>(), Arg.Any<CancellationToken>());
    }
}
