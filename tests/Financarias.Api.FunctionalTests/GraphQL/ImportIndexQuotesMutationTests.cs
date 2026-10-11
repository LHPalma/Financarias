using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.MarketData.Indexes.Import;
using Financarias.Domain.MarketData.Indexes;
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

public class ImportIndexQuotesMutationTests : IAsyncLifetime
{
    private const string Selection = "rowsRead quotesCreated quotesUpdated";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly IIndexQuoteProvider _provider = Substitute.For<IIndexQuoteProvider>();

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

                services.RemoveAll<IIndexQuoteProvider>();
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

    [Fact(DisplayName = "importIndexQuotes grava os fechamentos do ano e devolve os contadores")]
    public async Task ImportIndexQuotes_PersistsAndReportsCounters()
    {
        // Arrange
        ProviderReturns(Item(new DateOnly(2018, 10, 5), 82_321.52m), Item(new DateOnly(2018, 10, 8), 86_083.92m));

        // Act
        var data = await SendAsync($"mutation {{ importIndexQuotes(input: {{ index: IBOVESPA, year: 2018 }}) {{ {Selection} }} }}");

        // Assert
        var result = data.GetProperty("data").GetProperty("importIndexQuotes");
        Assert.Equal(2, result.GetProperty("rowsRead").GetInt32());
        Assert.Equal(2, result.GetProperty("quotesCreated").GetInt32());
        Assert.Equal(0, result.GetProperty("quotesUpdated").GetInt32());

        await _provider.Received(1).GetYearAsync(MarketIndex.Ibovespa, 2018, Arg.Any<CancellationToken>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        Assert.Equal(2, await db.IndexQuotes.CountAsync());
    }

    [Fact(DisplayName = "importIndexQuotes repetido não duplica: a segunda chamada cria zero")]
    public async Task ImportIndexQuotes_Twice_DoesNotDuplicate()
    {
        // Arrange
        ProviderReturns(Item(new DateOnly(2018, 10, 8), 86_083.92m));
        var mutation = $"mutation {{ importIndexQuotes(input: {{ index: IBOVESPA, year: 2018 }}) {{ {Selection} }} }}";
        await SendAsync(mutation);

        // Act
        var data = await SendAsync(mutation);

        // Assert
        var result = data.GetProperty("data").GetProperty("importIndexQuotes");
        Assert.Equal(1, result.GetProperty("rowsRead").GetInt32());
        Assert.Equal(0, result.GetProperty("quotesCreated").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        Assert.Equal(1, await db.IndexQuotes.CountAsync());
    }

    [Fact(DisplayName = "importIndexQuotes de um ano sem pregões devolve zeros, sem erro")]
    public async Task ImportIndexQuotes_ForYearWithoutData_ReturnsZeroes()
    {
        // Arrange
        ProviderReturns();

        // Act
        var data = await SendAsync($"mutation {{ importIndexQuotes(input: {{ index: IBOVESPA, year: 2040 }}) {{ {Selection} }} }}");

        // Assert
        var result = data.GetProperty("data").GetProperty("importIndexQuotes");
        Assert.Equal(0, result.GetProperty("rowsRead").GetInt32());
        Assert.False(data.TryGetProperty("errors", out _));
    }

    [Fact(DisplayName = "importIndexQuotes com ano anterior a 1968 devolve o código index.year.invalid")]
    public async Task ImportIndexQuotes_WithYearBeforeFirst_ReturnsDomainErrorCode()
    {
        // Act
        var data = await SendAsync($"mutation {{ importIndexQuotes(input: {{ index: IBOVESPA, year: 1967 }}) {{ {Selection} }} }}");

        // Assert
        var error = data.GetProperty("errors")[0];
        Assert.Equal("index.year.invalid", error.GetProperty("extensions").GetProperty("code").GetString());
        await _provider.DidNotReceive().GetYearAsync(Arg.Any<MarketIndex>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "O schema documenta a mutation, o enum de índice e o resultado")]
    public async Task Schema_DocumentsTheOperation()
    {
        // Act
        var sdl = await _factory.CreateClient().GetStringAsync("/graphql?sdl");

        // Assert
        Assert.Contains("importIndexQuotes(", sdl, StringComparison.Ordinal);
        Assert.Contains("enum MarketIndex", sdl, StringComparison.Ordinal);
        Assert.Contains("IBOVESPA", sdl, StringComparison.Ordinal);
        Assert.Contains("Ibovespa, o índice das ações mais negociadas da B3", sdl, StringComparison.Ordinal);
        Assert.Contains("Resultado da importação dos fechamentos diários de um índice", sdl, StringComparison.Ordinal);
    }

    private static IndexQuoteImportItem Item(DateOnly tradingDate, decimal closeValue) =>
        new(MarketIndex.Ibovespa, tradingDate, closeValue, IndexQuoteSource.B3);

    private async Task<JsonElement> SendAsync(string query)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/graphql", new { query });
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private void ProviderReturns(params IndexQuoteImportItem[] items) =>
        _provider.GetYearAsync(Arg.Any<MarketIndex>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(items);
}
