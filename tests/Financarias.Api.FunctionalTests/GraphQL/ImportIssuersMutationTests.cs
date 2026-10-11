using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.MarketData.Issuers.Import;
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

public class ImportIssuersMutationTests : IAsyncLifetime
{
    private const string Mutation = "mutation { importIssuers { issuersWithQuotes notListed created updated } }";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly IIssuerProvider _provider = Substitute.For<IIssuerProvider>();

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

                services.RemoveAll<IIssuerProvider>();
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

    [Fact(DisplayName = "importIssuers cadastra os emissores que têm cotação e devolve os contadores")]
    public async Task ImportIssuers_PersistsTheIssuersWithQuotes()
    {
        // Arrange
        await SeedQuoteAsync("PETR4", "BRPETRACNPR6");
        await SeedQuoteAsync("ARCZ6", "BRARCZACNPB5");
        _provider.GetAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new IssuerFetch(
                [new IssuerImportItem("PETR", "9512", "PETROLEO BRASILEIRO S.A. PETROBRAS", "PETROBRAS", "33000167000101", "Petróleo. Gás e Biocombustíveis", null, null, "BOVESPA NIVEL 2", null, null)],
                1));

        // Act
        var data = await SendAsync(Mutation);

        // Assert
        var result = data.GetProperty("data").GetProperty("importIssuers");
        Assert.Equal(2, result.GetProperty("issuersWithQuotes").GetInt32());
        Assert.Equal(1, result.GetProperty("notListed").GetInt32());
        Assert.Equal(1, result.GetProperty("created").GetInt32());
        Assert.Equal(0, result.GetProperty("updated").GetInt32());

        await _provider.Received(1).GetAsync(
            Arg.Is<IReadOnlyCollection<string>>(codes => codes.SequenceEqual(new[] { "ARCZ", "PETR" })),
            Arg.Any<CancellationToken>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        Assert.Equal("Petróleo. Gás e Biocombustíveis", (await db.Issuers.SingleAsync()).Sector);
    }

    [Fact(DisplayName = "importIssuers sem cotações importadas devolve zeros e não chama a B3")]
    public async Task ImportIssuers_WithoutQuotes_ReturnsZeroes()
    {
        // Act
        var data = await SendAsync(Mutation);

        // Assert
        var result = data.GetProperty("data").GetProperty("importIssuers");
        Assert.Equal(0, result.GetProperty("issuersWithQuotes").GetInt32());
        await _provider.DidNotReceiveWithAnyArgs().GetAsync(default!, default);
    }

    [Fact(DisplayName = "O schema documenta a mutation e o resultado do cadastro de emissores")]
    public async Task Schema_DocumentsTheOperation()
    {
        // Act
        var sdl = await _factory.CreateClient().GetStringAsync("/graphql?sdl");

        // Assert
        Assert.Contains("importIssuers", sdl, StringComparison.Ordinal);
        Assert.Contains("Resultado da importação do cadastro dos emissores", sdl, StringComparison.Ordinal);
    }

    private async Task SeedQuoteAsync(string ticker, string isin)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        db.DailyQuotes.Add(DailyQuote.Create(
            Ticker.Create(ticker), new DateOnly(2026, 10, 9), QuoteMarketType.Cash, "02", "EMPRESA", "PN", isin,
            10m, 11m, 9m, 10m, 10m, 100, 1000, 10_000m, 1));
        await db.SaveChangesAsync();
    }

    private async Task<JsonElement> SendAsync(string query)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/graphql", new { query });
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
