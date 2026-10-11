using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.MarketData.CorporateEvents.Import;
using Financarias.Domain.MarketData;
using Financarias.Domain.MarketData.CorporateEvents;
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

public class ImportCorporateEventsMutationTests : IAsyncLifetime
{
    private const string Selection = "issuerFound namesTried eventsRead eventsSkipped eventsCreated eventsRemoved";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly ICorporateEventProvider _provider = Substitute.For<ICorporateEventProvider>();

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

                services.RemoveAll<ICorporateEventProvider>();
                services.AddScoped(_ => _provider);
            });
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        await db.Database.MigrateAsync();

        db.DailyQuotes.Add(DailyQuote.Create(
            Ticker.Create("PETR4"), new DateOnly(2026, 10, 9), QuoteMarketType.Cash, "02", "PETROBRAS", "PN", "BRPETRACNPR6",
            38m, 39m, 37m, 38m, 38.5m, 100, 1000, 38_000m, 1));
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact(DisplayName = "importCorporateEvents grava os eventos do emissor e devolve os contadores")]
    public async Task ImportCorporateEvents_PersistsAndReportsCounters()
    {
        // Arrange
        ProviderReturns(skipped: 1, Cash(0.55m), Cash(0.55m), Split());

        // Act
        var data = await SendAsync($"mutation {{ importCorporateEvents(input: {{ issuer: \"PETR\" }}) {{ {Selection} }} }}");

        // Assert
        var result = data.GetProperty("data").GetProperty("importCorporateEvents");
        Assert.True(result.GetProperty("issuerFound").GetBoolean());
        Assert.Equal(1, result.GetProperty("namesTried").GetInt32());
        Assert.Equal(3, result.GetProperty("eventsRead").GetInt32());
        Assert.Equal(1, result.GetProperty("eventsSkipped").GetInt32());
        Assert.Equal(3, result.GetProperty("eventsCreated").GetInt32());
        Assert.Equal(0, result.GetProperty("eventsRemoved").GetInt32());

        await _provider.Received(1).GetAsync(
            "PETR",
            Arg.Is<IReadOnlyCollection<string>>(names => names.SequenceEqual(new[] { "PETROBRAS" })),
            Arg.Any<CancellationToken>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        Assert.Equal(3, await db.CorporateEvents.CountAsync());
    }

    [Fact(DisplayName = "importCorporateEvents repetido não altera nada, nem as parcelas idênticas")]
    public async Task ImportCorporateEvents_Twice_ChangesNothing()
    {
        // Arrange
        ProviderReturns(skipped: 0, Cash(0.55m), Cash(0.55m));
        var mutation = $"mutation {{ importCorporateEvents(input: {{ issuer: \"PETR\" }}) {{ {Selection} }} }}";
        await SendAsync(mutation);

        // Act
        var data = await SendAsync(mutation);

        // Assert
        var result = data.GetProperty("data").GetProperty("importCorporateEvents");
        Assert.Equal(2, result.GetProperty("eventsRead").GetInt32());
        Assert.Equal(0, result.GetProperty("eventsCreated").GetInt32());
        Assert.Equal(0, result.GetProperty("eventsRemoved").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        Assert.Equal(2, await db.CorporateEvents.CountAsync());
    }

    [Fact(DisplayName = "importCorporateEvents de emissor sem cotações devolve issuerFound falso, sem chamar a B3")]
    public async Task ImportCorporateEvents_ForIssuerWithoutQuotes_ReturnsNotFound()
    {
        // Act
        var data = await SendAsync($"mutation {{ importCorporateEvents(input: {{ issuer: \"ZZZZ\" }}) {{ {Selection} }} }}");

        // Assert
        var result = data.GetProperty("data").GetProperty("importCorporateEvents");
        Assert.False(result.GetProperty("issuerFound").GetBoolean());
        Assert.Equal(0, result.GetProperty("eventsRead").GetInt32());
        await _provider.DidNotReceive().GetAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "importCorporateEvents com código de emissor inválido devolve o código corporate.event.invalid")]
    public async Task ImportCorporateEvents_WithInvalidIssuer_ReturnsDomainErrorCode()
    {
        // Act
        var data = await SendAsync($"mutation {{ importCorporateEvents(input: {{ issuer: \"petr4\" }}) {{ {Selection} }} }}");

        // Assert
        var error = data.GetProperty("errors")[0];
        Assert.Equal("corporate.event.invalid", error.GetProperty("extensions").GetProperty("code").GetString());
        await _provider.DidNotReceive().GetAsync(
            Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "O schema documenta a mutation, o input e o resultado")]
    public async Task Schema_DocumentsTheOperation()
    {
        // Act
        var sdl = await _factory.CreateClient().GetStringAsync("/graphql?sdl");

        // Assert
        Assert.Contains("importCorporateEvents(", sdl, StringComparison.Ordinal);
        Assert.Contains("Código do emissor, quatro letras ou dígitos em maiúsculas", sdl, StringComparison.Ordinal);
        Assert.Contains("Resultado da importação dos eventos corporativos de um emissor", sdl, StringComparison.Ordinal);
        Assert.Contains("Parcelas idênticas contam uma a uma", sdl, StringComparison.Ordinal);
    }

    private static CorporateEventImportItem Cash(decimal amount) =>
        new(null, "PN", CorporateEventKind.CashDividend, new DateOnly(2026, 8, 21), null, null, amount, 1, null, "DIVIDENDO");

    private static CorporateEventImportItem Split() =>
        new("BRPETRACNPR6", "PN", CorporateEventKind.StockSplit, new DateOnly(2008, 4, 25), null, null, null, null, 100m, "DESDOBRAMENTO");

    private async Task<JsonElement> SendAsync(string query)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/graphql", new { query });
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private void ProviderReturns(int skipped, params CorporateEventImportItem[] items) =>
        _provider.GetAsync(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new CorporateEventFetch(items, skipped));
}
