using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.MarketData.Indicators.Import;
using Financarias.Domain.MarketData.Indicators;
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

public class ImportIndicatorValuesMutationTests : IAsyncLifetime
{
    private const string Selection = "rowsRead valuesCreated valuesUpdated";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly IIndicatorValueProvider _provider = Substitute.For<IIndicatorValueProvider>();

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

                services.RemoveAll<IIndicatorValueProvider>();
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

    [Fact(DisplayName = "importIndicatorValues grava os valores do ano e devolve os contadores")]
    public async Task ImportIndicatorValues_PersistsAndReportsCounters()
    {
        // Arrange
        ProviderReturns(Item(new DateOnly(2026, 10, 8), 5.0119m), Item(new DateOnly(2026, 10, 9), 4.9892m));

        // Act
        var data = await SendAsync($"mutation {{ importIndicatorValues(input: {{ indicator: DOLLAR, year: 2026 }}) {{ {Selection} }} }}");

        // Assert
        var result = data.GetProperty("data").GetProperty("importIndicatorValues");
        Assert.Equal(2, result.GetProperty("rowsRead").GetInt32());
        Assert.Equal(2, result.GetProperty("valuesCreated").GetInt32());
        Assert.Equal(0, result.GetProperty("valuesUpdated").GetInt32());

        await _provider.Received(1).GetYearAsync(EconomicIndicator.Dollar, 2026, Arg.Any<CancellationToken>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        Assert.Equal(2, await db.IndicatorValues.CountAsync());
    }

    [Fact(DisplayName = "importIndicatorValues repetido não duplica: a segunda chamada cria zero")]
    public async Task ImportIndicatorValues_Twice_DoesNotDuplicate()
    {
        // Arrange
        ProviderReturns(Item(new DateOnly(2026, 10, 9), 4.9892m));
        var mutation = $"mutation {{ importIndicatorValues(input: {{ indicator: DOLLAR, year: 2026 }}) {{ {Selection} }} }}";
        await SendAsync(mutation);

        // Act
        var data = await SendAsync(mutation);

        // Assert
        var result = data.GetProperty("data").GetProperty("importIndicatorValues");
        Assert.Equal(1, result.GetProperty("rowsRead").GetInt32());
        Assert.Equal(0, result.GetProperty("valuesCreated").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        Assert.Equal(1, await db.IndicatorValues.CountAsync());
    }

    [Fact(DisplayName = "importIndicatorValues de um ano sem dados devolve zeros, sem erro")]
    public async Task ImportIndicatorValues_ForYearWithoutData_ReturnsZeroes()
    {
        // Arrange
        ProviderReturns();

        // Act
        var data = await SendAsync($"mutation {{ importIndicatorValues(input: {{ indicator: SELIC_TARGET, year: 1985 }}) {{ {Selection} }} }}");

        // Assert
        var result = data.GetProperty("data").GetProperty("importIndicatorValues");
        Assert.Equal(0, result.GetProperty("rowsRead").GetInt32());
        Assert.False(data.TryGetProperty("errors", out _));
    }

    [Fact(DisplayName = "importIndicatorValues com ano anterior a 1980 devolve o código indicator.year.invalid")]
    public async Task ImportIndicatorValues_WithYearBeforeFirst_ReturnsDomainErrorCode()
    {
        // Act
        var data = await SendAsync($"mutation {{ importIndicatorValues(input: {{ indicator: IPCA, year: 1979 }}) {{ {Selection} }} }}");

        // Assert
        var error = data.GetProperty("errors")[0];
        Assert.Equal("indicator.year.invalid", error.GetProperty("extensions").GetProperty("code").GetString());
        await _provider.DidNotReceive().GetYearAsync(Arg.Any<EconomicIndicator>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "O schema documenta a mutation, o enum de indicadores e o resultado")]
    public async Task Schema_DocumentsTheOperation()
    {
        // Act
        var sdl = await _factory.CreateClient().GetStringAsync("/graphql?sdl");

        // Assert
        Assert.Contains("importIndicatorValues(", sdl, StringComparison.Ordinal);
        Assert.Contains("enum EconomicIndicator", sdl, StringComparison.Ordinal);
        Assert.Contains("SELIC_TARGET", sdl, StringComparison.Ordinal);
        Assert.Contains("moeda da época", sdl, StringComparison.Ordinal);
        Assert.Contains("Resultado da importação dos valores de um indicador econômico", sdl, StringComparison.Ordinal);
    }

    private static IndicatorValueImportItem Item(DateOnly referenceDate, decimal value) =>
        new(EconomicIndicator.Dollar, referenceDate, value);

    private async Task<JsonElement> SendAsync(string query)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/graphql", new { query });
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    private void ProviderReturns(params IndicatorValueImportItem[] items) =>
        _provider.GetYearAsync(Arg.Any<EconomicIndicator>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(items);
}
