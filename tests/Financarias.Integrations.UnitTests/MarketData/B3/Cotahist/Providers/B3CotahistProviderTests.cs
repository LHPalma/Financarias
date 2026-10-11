using System.IO.Compression;
using System.Net;
using System.Text;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Stocks;
using Financarias.Integrations.MarketData.B3.Cotahist.Clients;
using Financarias.Integrations.MarketData.B3.Cotahist.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Refit;

namespace Financarias.Integrations.UnitTests.MarketData.B3.Cotahist.Providers;

public class B3CotahistProviderTests
{
    // Linhas reais do COTAHIST_D09102026 (registro 01, 245 caracteres).
    private const string Petr4Line =
        "012026100902PETR4       010PETROBRAS   PN      N2   R$  000000000554500000000056800000000005510000000000560700000000056000000000005600000000000560291623000000000049809400000000279303989300000000000000009999123100000010000000000000BRPETRACNPR6230";

    private const string OptionLine =
        "012026100978ABCBJ239    070ABCB        PN      N2000R$  000000000052000000000005200000000000520000000000052000000000005200000000000000000000000000000002000000000000003300000000000001716000000000000239002026101600000010000000000000BRABCBACNPR4151";

    private static readonly QuotePeriod DayPeriod = QuotePeriod.ForDay(new DateOnly(2026, 10, 9));

    private readonly IB3CotahistClient _client = Substitute.For<IB3CotahistClient>();
    private readonly ListLogger<B3CotahistProvider> _logger = new();
    private readonly B3CotahistProvider _provider;

    public B3CotahistProviderTests()
    {
        _provider = new B3CotahistProvider(_client, _logger);
    }

    public static TheoryData<QuotePeriod, string> PeriodsAndFileNames => new()
    {
        { QuotePeriod.ForDay(new DateOnly(2026, 10, 9)), "COTAHIST_D09102026.ZIP" },
        { QuotePeriod.ForDay(new DateOnly(2026, 1, 2)), "COTAHIST_D02012026.ZIP" },
        { QuotePeriod.ForMonth(2026, 9), "COTAHIST_M092026.ZIP" },
        { QuotePeriod.ForMonth(2024, 12), "COTAHIST_M122024.ZIP" },
        { QuotePeriod.ForYear(2025), "COTAHIST_A2025.ZIP" },
        { QuotePeriod.ForYear(1986), "COTAHIST_A1986.ZIP" },
    };

    [Theory(DisplayName = "OpenAsync pede ao cliente o arquivo diário, mensal ou anual conforme o período")]
    [MemberData(nameof(PeriodsAndFileNames))]
    public async Task OpenAsync_RequestsTheFileNamedAfterThePeriod(QuotePeriod period, string expectedFileName)
    {
        // Arrange
        GivenClientReturns(Zip("COTAHIST_X.TXT", Header, Trailer(0)));

        // Act
        await using var file = await _provider.OpenAsync(period);

        // Assert
        await _client.Received(1).GetFileAsync(expectedFileName, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "OpenAsync devolve nulo quando o arquivo não existe (404)")]
    public async Task OpenAsync_ReturnsNull_On404()
    {
        // Arrange
        var notFound = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "https://bvmf.bmfbovespa.com.br/x.zip"),
            HttpMethod.Get,
            new HttpResponseMessage(HttpStatusCode.NotFound),
            new RefitSettings());

        _client.GetFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<Stream>(_ => throw notFound);

        // Act
        var file = await _provider.OpenAsync(DayPeriod);

        // Assert
        Assert.Null(file);
    }

    [Fact(DisplayName = "OpenAsync propaga falha de transporte diferente de 404")]
    public async Task OpenAsync_Propagates_NonNotFoundFailures()
    {
        // Arrange
        var serverError = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "https://bvmf.bmfbovespa.com.br/x.zip"),
            HttpMethod.Get,
            new HttpResponseMessage(HttpStatusCode.InternalServerError),
            new RefitSettings());

        _client.GetFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<Stream>(_ => throw serverError);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => _provider.OpenAsync(DayPeriod));
    }

    [Fact(DisplayName = "ReadAsync devolve só as cotações de escopo e conta as malformadas como puladas")]
    public async Task ReadAsync_YieldsInScopeQuotes_AndCountsSkippedRows()
    {
        // Arrange: ação válida, opção (fora de escopo), linha de escopo truncada (malformada)
        GivenClientReturns(Zip(
            "COTAHIST_D09102026.TXT",
            Header,
            Petr4Line,
            OptionLine,
            Petr4Line[..244],
            Trailer(3)));

        await using var file = await OpenFileAsync();

        // Act
        var items = await ReadAllAsync(file);

        // Assert
        var item = Assert.Single(items);
        Assert.Equal("PETR4", item.Ticker.Value);
        Assert.Equal(QuoteMarketType.Cash, item.MarketType);
        Assert.Equal(1, file.RowsSkipped);
        Assert.Empty(_logger.Warnings);
    }

    [Fact(DisplayName = "ReadAsync decodifica o arquivo como Latin1 (acentos do nome resumido)")]
    public async Task ReadAsync_DecodesLatin1()
    {
        // Arrange: NOMRES (posições 28–39) com acentos, gravado em Latin1
        var line = Petr4Line[..27] + "AÇÃO TESTE  " + Petr4Line[39..];
        GivenClientReturns(Zip("COTAHIST_D09102026.TXT", Header, line, Trailer(1)));

        await using var file = await OpenFileAsync();

        // Act
        var items = await ReadAllAsync(file);

        // Assert
        Assert.Equal("AÇÃO TESTE", Assert.Single(items).CompanyName);
    }

    [Fact(DisplayName = "ReadAsync avisa quando o total do trailer não bate com os registros lidos")]
    public async Task ReadAsync_Warns_WhenTrailerTotalDoesNotMatch()
    {
        // Arrange
        GivenClientReturns(Zip("COTAHIST_D09102026.TXT", Header, Petr4Line, Trailer(99)));

        await using var file = await OpenFileAsync();

        // Act
        var items = await ReadAllAsync(file);

        // Assert
        Assert.Single(items);
        Assert.Contains(_logger.Warnings, warning => warning.Contains("trailer declares 99", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "ReadAsync avisa quando o arquivo termina sem trailer (download possivelmente truncado)")]
    public async Task ReadAsync_Warns_WhenTrailerIsMissing()
    {
        // Arrange
        GivenClientReturns(Zip("COTAHIST_D09102026.TXT", Header, Petr4Line));

        await using var file = await OpenFileAsync();

        // Act
        var items = await ReadAllAsync(file);

        // Assert
        Assert.Single(items);
        Assert.Contains(_logger.Warnings, warning => warning.Contains("no readable trailer", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "OpenAsync lança InvalidDataException quando o ZIP não tem entrada .TXT")]
    public async Task OpenAsync_Throws_WhenZipHasNoTxtEntry()
    {
        // Arrange
        GivenClientReturns(Zip("LEIAME.md", "nada aqui"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(() => _provider.OpenAsync(DayPeriod));
    }

    [Fact(DisplayName = "Descartar o arquivo apaga a cópia temporária do ZIP")]
    public async Task DisposeAsync_DeletesTheTemporaryCopy()
    {
        // Arrange
        GivenClientReturns(Zip("COTAHIST_D09102026.TXT", Header, Petr4Line, Trailer(1)));
        var before = TemporaryCopies();

        var file = await OpenFileAsync();
        var created = TemporaryCopies().Except(before).ToList();

        // Act
        await file.DisposeAsync();

        // Assert
        Assert.Single(created);
        Assert.False(File.Exists(created[0]));
    }

    private static string Header { get; } =
        "00COTAHIST.2026BOVESPA 20261009".PadRight(245);

    private static string Trailer(long totalRecords) =>
        ("99COTAHIST.2026BOVESPA 20261009" + totalRecords.ToString("00000000000")).PadRight(245);

    private static string[] TemporaryCopies() =>
        Directory.GetFiles(Path.GetTempPath(), "cotahist-*.zip");

    private static Stream Zip(string entryName, params string[] lines)
    {
        var zip = new MemoryStream();
        using (var archive = new ZipArchive(zip, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryName);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.Latin1);
            writer.Write(string.Join("\r\n", lines) + "\r\n");
        }

        zip.Position = 0;
        return zip;
    }

    private static async Task<List<DailyQuoteImportItem>> ReadAllAsync(IDailyQuoteFile file)
    {
        var items = new List<DailyQuoteImportItem>();
        await foreach (var item in file.ReadAsync())
        {
            items.Add(item);
        }

        return items;
    }

    private void GivenClientReturns(Stream zip) =>
        _client.GetFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(zip);

    private async Task<IDailyQuoteFile> OpenFileAsync() =>
        await _provider.OpenAsync(DayPeriod) ?? throw new InvalidOperationException("Expected a file.");

    private sealed class ListLogger<T> : ILogger<T>
    {
        private readonly List<(LogLevel Level, string Message)> _entries = [];

        public IEnumerable<string> Warnings =>
            _entries.Where(entry => entry.Level == LogLevel.Warning).Select(entry => entry.Message);

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            _entries.Add((logLevel, formatter(state, exception)));
    }
}
