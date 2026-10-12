using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.MarketData.SearchInterest.Import;
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

public class ImportSearchInterestMutationTests : IAsyncLifetime
{
    private const string Mutation =
        "mutation { importSearchInterest(input: { terms: [\"Americanas\", \"magalu\"], geo: \"BR\", from: \"2023-01-01\", to: \"2023-01-31\" }) { pointsRead pointsCreated pointsUpdated } }";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();
    private readonly ISearchInterestProvider _provider = Substitute.For<ISearchInterestProvider>();

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

                services.RemoveAll<ISearchInterestProvider>();
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

    [Fact(DisplayName = "importSearchInterest grava os pontos, normaliza os termos e devolve os contadores")]
    public async Task ImportSearchInterest_PersistsAndReportsCounters()
    {
        // Arrange
        _provider.GetAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([new SearchInterestImportItem("americanas", new DateOnly(2023, 1, 12), 100), new SearchInterestImportItem("magalu", new DateOnly(2023, 1, 12), 9)]);

        // Act
        var data = await SendAsync(Mutation);

        // Assert
        var result = data.GetProperty("data").GetProperty("importSearchInterest");
        Assert.Equal(2, result.GetProperty("pointsRead").GetInt32());
        Assert.Equal(2, result.GetProperty("pointsCreated").GetInt32());
        Assert.Equal(0, result.GetProperty("pointsUpdated").GetInt32());

        await _provider.Received(1).GetAsync(
            Arg.Is<IReadOnlyList<string>>(terms => terms.SequenceEqual(new[] { "americanas", "magalu" })),
            "BR",
            new DateOnly(2023, 1, 1),
            new DateOnly(2023, 1, 31),
            Arg.Any<CancellationToken>());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinancariasDbContext>();
        Assert.Equal(2, await db.SearchInterestPoints.CountAsync());
    }

    [Fact(DisplayName = "importSearchInterest com mais de cinco termos devolve o código searchinterest.point.invalid")]
    public async Task ImportSearchInterest_WithTooManyTerms_ReturnsDomainErrorCode()
    {
        // Act
        var data = await SendAsync(
            "mutation { importSearchInterest(input: { terms: [\"a\",\"b\",\"c\",\"d\",\"e\",\"f\"], geo: \"BR\", from: \"2023-01-01\", to: \"2023-01-31\" }) { pointsRead } }");

        // Assert
        var error = data.GetProperty("errors")[0];
        Assert.Equal("searchinterest.point.invalid", error.GetProperty("extensions").GetProperty("code").GetString());
        await _provider.DidNotReceiveWithAnyArgs().GetAsync(default!, default!, default, default, default);
    }

    [Fact(DisplayName = "O schema documenta a mutation e o resultado da importação do interesse de busca")]
    public async Task Schema_DocumentsTheOperation()
    {
        // Act
        var sdl = await _factory.CreateClient().GetStringAsync("/graphql?sdl");

        // Assert
        Assert.Contains("importSearchInterest", sdl, StringComparison.Ordinal);
        Assert.Contains("Resultado da importação do interesse de busca", sdl, StringComparison.Ordinal);
        Assert.Contains("relativo ao grupo", sdl, StringComparison.Ordinal);
    }

    private async Task<JsonElement> SendAsync(string query)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/graphql", new { query });
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
