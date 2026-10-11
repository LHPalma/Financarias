using System.Net.Http.Json;
using System.Text.Json;
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
using Testcontainers.PostgreSql;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class RebuildInflationLevelsMutationTests : IAsyncLifetime
{
    private const string Mutation =
        "mutation { rebuildInflationLevels { firstDay lastDay days levelsCreated levelsUpdated levelsRemoved } }";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

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

    [Fact(DisplayName = "rebuildInflationLevels grava os níveis diários e devolve os contadores")]
    public async Task RebuildInflationLevels_PersistsTheLevelsAndReportsCounters()
    {
        // Arrange
        await SeedIpcaAsync(10m, 20m, 5m);

        // Act
        var data = await SendAsync(Mutation);

        // Assert
        var result = data.GetProperty("data").GetProperty("rebuildInflationLevels");
        Assert.Equal("2024-01-15", result.GetProperty("firstDay").GetString());
        Assert.Equal("2024-04-15", result.GetProperty("lastDay").GetString());
        Assert.Equal(92, result.GetProperty("days").GetInt32());
        Assert.Equal(92, result.GetProperty("levelsCreated").GetInt32());
        Assert.Equal(0, result.GetProperty("levelsRemoved").GetInt32());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        Assert.Equal(92, await db.InflationLevels.CountAsync());
    }

    [Fact(DisplayName = "rebuildInflationLevels sem IPCA importado devolve o código inflation.series.invalid")]
    public async Task RebuildInflationLevels_WithoutIpca_ReturnsDomainErrorCode()
    {
        // Act
        var data = await SendAsync(Mutation);

        // Assert
        var error = data.GetProperty("errors")[0];
        Assert.Equal("inflation.series.invalid", error.GetProperty("extensions").GetProperty("code").GetString());
    }

    [Fact(DisplayName = "O schema documenta a mutation e o resultado da reconstrução do índice")]
    public async Task Schema_DocumentsTheOperation()
    {
        // Act
        var sdl = await _factory.CreateClient().GetStringAsync("/graphql?sdl");

        // Assert
        Assert.Contains("rebuildInflationLevels", sdl, StringComparison.Ordinal);
        Assert.Contains("metodologia da B3", sdl, StringComparison.Ordinal);
    }

    private async Task SeedIpcaAsync(params decimal[] ratesFromJanuary2024)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        db.IndicatorValues.AddRange(ratesFromJanuary2024.Select((rate, i) =>
            IndicatorValue.Create(EconomicIndicator.Ipca, new DateOnly(2024, 1 + i, 1), rate)));
        await db.SaveChangesAsync();
    }

    private async Task<JsonElement> SendAsync(string query)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/graphql", new { query });
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
