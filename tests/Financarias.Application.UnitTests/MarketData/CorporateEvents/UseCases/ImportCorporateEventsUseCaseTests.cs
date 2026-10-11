using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.CorporateEvents.Commands;
using Financarias.Application.MarketData.CorporateEvents.DTOs.Requests;
using Financarias.Application.MarketData.CorporateEvents.Import;
using Financarias.Application.MarketData.CorporateEvents.Queries;
using Financarias.Application.MarketData.CorporateEvents.UseCases;
using Financarias.Domain.Common.Exceptions;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.CorporateEvents.UseCases;

public class ImportCorporateEventsUseCaseTests
{
    private static readonly CorporateEventImportResult Result = new(true, 2, 30, 1, 28, 0);

    private readonly IIssuerReads _issuerReads = Substitute.For<IIssuerReads>();

    private readonly ICommandHandler<ImportCorporateEventsCommand, CorporateEventImportResult> _handler =
        Substitute.For<ICommandHandler<ImportCorporateEventsCommand, CorporateEventImportResult>>();

    private readonly ImportCorporateEventsUseCase _useCase;

    public ImportCorporateEventsUseCaseTests()
    {
        _handler.HandleAsync(Arg.Any<ImportCorporateEventsCommand>(), Arg.Any<CancellationToken>()).Returns(Result);
        _useCase = new ImportCorporateEventsUseCase(_issuerReads, _handler);
    }

    [Fact(DisplayName = "Busca os nomes de pregão do emissor e entrega o comando ao handler")]
    public async Task Execute_ForKnownIssuer_ForwardsTheNamesToTheHandler()
    {
        // Arrange
        _issuerReads.TradingNamesAsync("SDIA", Arg.Any<CancellationToken>())
            .Returns(new[] { "SADIA S.A.", "SADIA S/A" });

        // Act
        var result = await _useCase.ExecuteAsync(new ImportCorporateEventsRequest("SDIA"));

        // Assert
        Assert.Same(Result, result);
        await _handler.Received(1).HandleAsync(
            Arg.Is<ImportCorporateEventsCommand>(command =>
                command.IssuerCode == "SDIA" && command.TradingNames.SequenceEqual(new[] { "SADIA S.A.", "SADIA S/A" })),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Emissor sem cotações importadas devolve resultado zerado sem chamar o handler")]
    public async Task Execute_ForIssuerWithoutQuotes_ReturnsZeroesWithoutCallingTheHandler()
    {
        // Arrange
        _issuerReads.TradingNamesAsync("ZZZZ", Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());

        // Act
        var result = await _useCase.ExecuteAsync(new ImportCorporateEventsRequest("ZZZZ"));

        // Assert
        Assert.Equal(new CorporateEventImportResult(false, 0, 0, 0, 0, 0), result);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ImportCorporateEventsCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "Código de emissor inválido lança erro de domínio sem consultar nada")]
    [InlineData("")]
    [InlineData("PET")]
    [InlineData("PETRO")]
    [InlineData("petr")]
    [InlineData("PE R")]
    public async Task Execute_WithInvalidIssuer_ThrowsBeforeAnyLookup(string issuer)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _useCase.ExecuteAsync(new ImportCorporateEventsRequest(issuer)));

        Assert.Equal("corporate.event.invalid", exception.Code);
        await _issuerReads.DidNotReceive().TradingNamesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ImportCorporateEventsCommand>(), Arg.Any<CancellationToken>());
    }
}
