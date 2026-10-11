using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Financarias.Application.MarketData.Stocks.Import;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.Stocks;
using Financarias.Infrastructure.Persistence;
using Financarias.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class ImportDailyQuotesMutationTests : IAsyncLifetime
{
    private const string Selection = "fileFound rowsRead rowsSkipped quotesCreated quotesUpdated";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly IDailyQuoteProvider _provider = Substitute.For<IDailyQuoteProvider>();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Integrations:ViaCep:BaseUrl"] = "https://viacep.com.br/ws",
                    ["Integrations:Anbima:BaseUrl"] = "https://www.anbima.com.br"
                }));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<FinancariasDbContext>>();

                services.AddDbContext<FinancariasDbContext>((provider, options) =>
                    options
                        .UseNpgsql(_postgres.GetConnectionString())
                        .UseSnakeCaseNamingConvention()
                        .AddInterceptors(provider.GetRequiredService<AuditableEntityInterceptor>()));

                services.RemoveAll<IDailyQuoteProvider>();
                services.AddScoped(_ => _provider);
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact(DisplayName = "importDailyQuotes com um dia grava as cotações e devolve os contadores")]
    public async Task ImportDailyQuotes_WithDay_PersistsAndReportsCounters()
    {
        // Arrange
        ProviderReturns(Item("PETR4"), Item("VALE3"));

        // Act
        var data = await SendAsync($"mutation {{ importDailyQuotes(input: {{ day: \"2026-10-09\" }}) {{ {Selection} }} }}");

        // Assert
        var result = data.GetProperty("data").GetProperty("importDailyQuotes");
        Assert.True(result.GetProperty("fileFound").GetBoolean());
        Assert.Equal(2, result.GetProperty("rowsRead").GetInt32());
        Assert.Equal(2, result.GetProperty("quotesCreated").GetInt32());
        Assert.Equal(0, result.GetProperty("quotesUpdated").GetInt32());

        await _provider.Received(1).OpenAsync(
            QuotePeriod.ForDay(new DateOnly(2026, 10, 9)),
            Arg.Any<CancellationToken>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        Assert.Equal(2, await db.DailyQuotes.CountAsync());
    }

    [Fact(DisplayName = "importDailyQuotes com mês e com ano pede ao provedor o período correspondente")]
    public async Task ImportDailyQuotes_WithMonthAndYear_AsksTheProviderForThePeriod()
    {
        // Arrange
        ProviderReturnsNoFile();

        // Act
        await SendAsync($"mutation {{ importDailyQuotes(input: {{ month: {{ year: 2026, month: 9 }} }}) {{ {Selection} }} }}");
        await SendAsync($"mutation {{ importDailyQuotes(input: {{ year: 2025 }}) {{ {Selection} }} }}");

        // Assert
        await _provider.Received(1).OpenAsync(QuotePeriod.ForMonth(2026, 9), Arg.Any<CancellationToken>());
        await _provider.Received(1).OpenAsync(QuotePeriod.ForYear(2025), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "importDailyQuotes sem arquivo na B3 devolve fileFound falso e zeros")]
    public async Task ImportDailyQuotes_WithoutFile_ReturnsFileNotFound()
    {
        // Arrange
        ProviderReturnsNoFile();

        // Act
        var data = await SendAsync($"mutation {{ importDailyQuotes(input: {{ day: \"2026-10-10\" }}) {{ {Selection} }} }}");

        // Assert
        var result = data.GetProperty("data").GetProperty("importDailyQuotes");
        Assert.False(result.GetProperty("fileFound").GetBoolean());
        Assert.Equal(0, result.GetProperty("rowsRead").GetInt32());
        Assert.Equal(0, result.GetProperty("quotesCreated").GetInt32());
    }

    [Fact(DisplayName = "importDailyQuotes recusa mais de um campo do período antes de chamar o provedor (@oneOf)")]
    public async Task ImportDailyQuotes_WithMoreThanOneField_IsRejectedBySchema()
    {
        // Act
        var data = await SendAsync(
            $"mutation {{ importDailyQuotes(input: {{ day: \"2026-10-09\", year: 2026 }}) {{ {Selection} }} }}");

        // Assert
        Assert.True(data.TryGetProperty("errors", out var errors), data.ToString());
        Assert.NotEmpty(errors.EnumerateArray());
        Assert.Contains("oneof", errors[0].GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
        await _provider.DidNotReceive().OpenAsync(Arg.Any<QuotePeriod>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "importDailyQuotes recusa input vazio antes de chamar o provedor (@oneOf)")]
    public async Task ImportDailyQuotes_WithNoField_IsRejectedBySchema()
    {
        // Act
        var data = await SendAsync($"mutation {{ importDailyQuotes(input: {{ }}) {{ {Selection} }} }}");

        // Assert
        Assert.True(data.TryGetProperty("errors", out var errors));
        Assert.NotEmpty(errors.EnumerateArray());
        await _provider.DidNotReceive().OpenAsync(Arg.Any<QuotePeriod>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "importDailyQuotes com ano anterior a 1986 devolve o código stock.quoteperiod.invalid")]
    public async Task ImportDailyQuotes_WithYearBeforeFirstFile_ReturnsDomainErrorCode()
    {
        // Act
        var data = await SendAsync($"mutation {{ importDailyQuotes(input: {{ year: 1985 }}) {{ {Selection} }} }}");

        // Assert
        var error = data.GetProperty("errors")[0];
        Assert.Equal("stock.quoteperiod.invalid", error.GetProperty("extensions").GetProperty("code").GetString());
        await _provider.DidNotReceive().OpenAsync(Arg.Any<QuotePeriod>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "O schema declara o input do período como @oneOf")]
    public async Task Schema_DeclaresTheImportInputAsOneOf()
    {
        // Act
        var sdl = await _factory.CreateClient().GetStringAsync("/graphql?sdl");

        // Assert
        Assert.Contains("input ImportDailyQuotesRequestInput @oneOf", sdl, StringComparison.Ordinal);
    }

    private static DailyQuoteImportItem Item(string ticker) =>
        new(
            Ticker.Create(ticker),
            new DateOnly(2026, 10, 9),
            QuoteMarketType.Cash,
            "02",
            "EMPRESA",
            "ON",
            "BRXXXXACNOR0",
            38.10m,
            38.90m,
            37.80m,
            38.40m,
            38.70m,
            52_310,
            41_250_300,
            1_583_000_000.55m,
            1);

    private async Task<JsonElement> SendAsync(string query)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/graphql", new { query });
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private void ProviderReturnsNoFile() =>
        _provider.OpenAsync(Arg.Any<QuotePeriod>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDailyQuoteFile?>(null));

    private void ProviderReturns(params DailyQuoteImportItem[] items)
    {
        var file = Substitute.For<IDailyQuoteFile>();
        file.ReadAsync(Arg.Any<CancellationToken>()).Returns(ToAsync(items));
        _provider.OpenAsync(Arg.Any<QuotePeriod>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDailyQuoteFile?>(file));
    }

    private static async IAsyncEnumerable<DailyQuoteImportItem> ToAsync(
        DailyQuoteImportItem[] items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            yield return item;
        }

        await Task.CompletedTask;
    }
}
