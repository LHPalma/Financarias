using Financarias.Application.Common.Messaging;
using Financarias.Application.MarketData.Stocks.Commands;
using Financarias.Application.MarketData.Stocks.DTOs.Requests;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Application.MarketData.Stocks.UseCases;
using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.MarketData;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.Stocks.UseCases;

public class ImportDailyQuotesUseCaseTests
{
    private static readonly DailyQuoteImportResult Result = new(true, 10, 1, 8, 2);

    private readonly ICommandHandler<ImportDailyQuotesCommand, DailyQuoteImportResult> _handler =
        Substitute.For<ICommandHandler<ImportDailyQuotesCommand, DailyQuoteImportResult>>();

    private readonly ImportDailyQuotesUseCase _useCase;

    public ImportDailyQuotesUseCaseTests()
    {
        _handler.HandleAsync(Arg.Any<ImportDailyQuotesCommand>(), Arg.Any<CancellationToken>()).Returns(Result);
        _useCase = new ImportDailyQuotesUseCase(_handler);
    }

    [Fact(DisplayName = "Um dia vira o período diário e o resultado do handler é devolvido")]
    public async Task Execute_WithDay_ImportsTheDailyPeriod()
    {
        // Act
        var result = await _useCase.ExecuteAsync(new ImportDailyQuotesRequest(new DateOnly(2026, 10, 9), null, null));

        // Assert
        Assert.Same(Result, result);
        await _handler.Received(1).HandleAsync(
            Arg.Is<ImportDailyQuotesCommand>(command => command.Period == QuotePeriod.ForDay(new DateOnly(2026, 10, 9))),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Um mês vira o período mensal")]
    public async Task Execute_WithMonth_ImportsTheMonthlyPeriod()
    {
        // Act
        await _useCase.ExecuteAsync(new ImportDailyQuotesRequest(null, new QuoteMonthRequest(2026, 9), null));

        // Assert
        await _handler.Received(1).HandleAsync(
            Arg.Is<ImportDailyQuotesCommand>(command => command.Period == QuotePeriod.ForMonth(2026, 9)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Um ano vira o período anual")]
    public async Task Execute_WithYear_ImportsTheYearlyPeriod()
    {
        // Act
        await _useCase.ExecuteAsync(new ImportDailyQuotesRequest(null, null, 2025));

        // Assert
        await _handler.Received(1).HandleAsync(
            Arg.Is<ImportDailyQuotesCommand>(command => command.Period == QuotePeriod.ForYear(2025)),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Nenhum campo informado lança erro de domínio sem chamar o handler")]
    public async Task Execute_WithNoField_ThrowsWithoutCallingTheHandler()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _useCase.ExecuteAsync(new ImportDailyQuotesRequest(null, null, null)));

        Assert.Equal("stock.quoteperiod.invalid", exception.Code);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ImportDailyQuotesCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Mais de um campo informado lança erro de domínio sem chamar o handler")]
    public async Task Execute_WithMoreThanOneField_ThrowsWithoutCallingTheHandler()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _useCase.ExecuteAsync(new ImportDailyQuotesRequest(new DateOnly(2026, 10, 9), null, 2026)));

        Assert.Equal("stock.quoteperiod.invalid", exception.Code);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ImportDailyQuotesCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Período fora do intervalo aceito lança erro de domínio sem chamar o handler")]
    public async Task Execute_WithInvalidPeriod_ThrowsWithoutCallingTheHandler()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _useCase.ExecuteAsync(new ImportDailyQuotesRequest(null, new QuoteMonthRequest(2026, 13), null)));

        Assert.Equal("stock.quoteperiod.invalid", exception.Code);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ImportDailyQuotesCommand>(), Arg.Any<CancellationToken>());
    }
}
