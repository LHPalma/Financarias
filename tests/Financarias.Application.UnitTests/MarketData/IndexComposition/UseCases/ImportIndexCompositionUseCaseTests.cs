using Financarias.Application.Common.Messaging;
using Financarias.Application.Common.Persistence;
using Financarias.Application.MarketData.IndexComposition.Commands;
using Financarias.Application.MarketData.IndexComposition.DTOs.Requests;
using Financarias.Application.MarketData.IndexComposition.Import;
using Financarias.Application.MarketData.IndexComposition.UseCases;
using Financarias.Domain.Common.Exceptions;
using NSubstitute;

namespace Financarias.Application.UnitTests.MarketData.IndexComposition.UseCases;

public class ImportIndexCompositionUseCaseTests
{
    private const string First = "https://www.b3.com.br/data/files/AA/BB/prv.zip";
    private const string Second = "https://www.b3.com.br/data/files/CC/DD/virada.xlsx";

    private readonly ICommandHandler<ImportIndexCompositionFileCommand, IndexCompositionFileImportResult> _handler =
        Substitute.For<ICommandHandler<ImportIndexCompositionFileCommand, IndexCompositionFileImportResult>>();

    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private readonly ImportIndexCompositionUseCase _useCase;

    public ImportIndexCompositionUseCaseTests()
    {
        _useCase = new ImportIndexCompositionUseCase(_handler, _unitOfWork);
    }

    [Fact(DisplayName = "Soma o resultado de cada arquivo importado")]
    public async Task Execute_SumsTheFiles()
    {
        // Arrange
        _handler.HandleAsync(new ImportIndexCompositionFileCommand(First), Arg.Any<CancellationToken>())
            .Returns(new IndexCompositionFileImportResult(23, 1800, 1800, 0));
        _handler.HandleAsync(new ImportIndexCompositionFileCommand(Second), Arg.Any<CancellationToken>())
            .Returns(new IndexCompositionFileImportResult(24, 1900, 1000, 5));

        // Act
        var result = await _useCase.ExecuteAsync(new ImportIndexCompositionRequest([First, Second]));

        // Assert
        Assert.Equal(2, result.FilesRead);
        Assert.Equal(47, result.Portfolios);
        Assert.Equal(3700, result.Rows);
        Assert.Equal(2800, result.Created);
        Assert.Equal(5, result.Updated);
        Assert.Empty(result.Failures);
    }

    [Fact(DisplayName = "Um arquivo que falha entra no resultado e os demais seguem")]
    public async Task Execute_ReportsTheFailureAndContinues()
    {
        // Arrange
        _handler.HandleAsync(new ImportIndexCompositionFileCommand(First), Arg.Any<CancellationToken>())
            .Returns<IndexCompositionFileImportResult>(_ => throw new InvalidDataException("zip corrompido"));
        _handler.HandleAsync(new ImportIndexCompositionFileCommand(Second), Arg.Any<CancellationToken>())
            .Returns(new IndexCompositionFileImportResult(1, 70, 70, 0));

        // Act
        var result = await _useCase.ExecuteAsync(new ImportIndexCompositionRequest([First, Second]));

        // Assert
        Assert.Equal(1, result.FilesRead);
        Assert.Equal(70, result.Created);
        var failure = Assert.Single(result.Failures);
        Assert.Equal(First, failure.Url);
        Assert.Equal("zip corrompido", failure.Reason);
        _unitOfWork.Received(1).ClearTracking();
    }

    [Fact(DisplayName = "Importa uma única vez o endereço repetido")]
    public async Task Execute_DeduplicatesUrls()
    {
        // Arrange
        _handler.HandleAsync(Arg.Any<ImportIndexCompositionFileCommand>(), Arg.Any<CancellationToken>())
            .Returns(new IndexCompositionFileImportResult(1, 1, 1, 0));

        // Act
        await _useCase.ExecuteAsync(new ImportIndexCompositionRequest([First, First, $" {First} "]));

        // Assert
        await _handler.Received(1).HandleAsync(Arg.Any<ImportIndexCompositionFileCommand>(), Arg.Any<CancellationToken>());
    }

    [Theory(DisplayName = "Endereço que não é um arquivo de dados da B3 lança erro de domínio sem chamar o handler")]
    [InlineData("http://www.b3.com.br/data/files/AA/prv.zip")]
    [InlineData("https://evil.example.com/data/files/AA/prv.zip")]
    [InlineData("https://www.b3.com.br/pt_br/noticias/prv.htm")]
    [InlineData("https://www.b3.com.br.evil.example.com/data/files/prv.zip")]
    [InlineData("data/files/prv.zip")]
    public async Task Execute_WithForeignUrl_ThrowsDomainError(string url)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() =>
            _useCase.ExecuteAsync(new ImportIndexCompositionRequest([First, url])));

        Assert.Equal("indexcomposition.source.invalid", exception.Code);
        await _handler.DidNotReceive().HandleAsync(Arg.Any<ImportIndexCompositionFileCommand>(), Arg.Any<CancellationToken>());
    }
}
