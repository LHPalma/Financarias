using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.CorporateEvents.Queries;
using Financarias.Application.MarketData.Issuers.Commands;
using Financarias.Application.MarketData.Issuers.Import;
using Financarias.Application.MarketData.Issuers.UseCases;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Issuers.UseCases;

public class ImportIssuersUseCaseTests
{
    private readonly IIssuerReads _reads = Substitute.For<IIssuerReads>();
    private readonly ICommandHandler<ImportIssuersCommand, IssuerImportResult> _handler =
        Substitute.For<ICommandHandler<ImportIssuersCommand, IssuerImportResult>>();

    [Fact(DisplayName = "Passa ao handler os emissores que têm cotação e devolve o resultado")]
    public async Task Execute_PassesTheIssuersWithQuotes()
    {
        // Arrange
        _reads.IssuerCodesAsync(Arg.Any<CancellationToken>()).Returns(["PETR", "VALE"]);
        var expected = new IssuerImportResult(2, 0, 2, 0);
        _handler.HandleAsync(Arg.Any<ImportIssuersCommand>(), Arg.Any<CancellationToken>()).Returns(expected);

        // Act
        var result = await new ImportIssuersUseCase(_reads, _handler).ExecuteAsync();

        // Assert
        Assert.Equal(expected, result);
        await _handler.Received(1).HandleAsync(
            Arg.Is<ImportIssuersCommand>(command => command.IssuerCodes.SequenceEqual(new[] { "PETR", "VALE" })),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Sem cotações importadas devolve zeros e não chama o handler")]
    public async Task Execute_WithoutQuotes_ReturnsZeroes()
    {
        // Arrange
        _reads.IssuerCodesAsync(Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await new ImportIssuersUseCase(_reads, _handler).ExecuteAsync();

        // Assert
        Assert.Equal(new IssuerImportResult(0, 0, 0, 0), result);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ImportIssuersCommand>(), Arg.Any<CancellationToken>());
    }
}
