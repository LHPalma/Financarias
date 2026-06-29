using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.Analytics.DTOs.Results;
using Financarias.Application.Analytics.Queries;
using Financarias.Application.Common.Messaging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class CalculateNtnbPriceQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IQueryHandler<CalculateNtnbPriceQuery, NtnbPriceResult> _handler =
        Substitute.For<IQueryHandler<CalculateNtnbPriceQuery, NtnbPriceResult>>();

    private readonly WebApplicationFactory<Program> _factory;

    public CalculateNtnbPriceQueryTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Integrations:ViaCep:BaseUrl"] = "https://viacep.com.br/ws",
                    ["Integrations:Anbima:BaseUrl"] = "https://www.anbima.com.br"
                }));

            // Stub do handler: a query não toca o banco; o mapper (real) ainda roda antes dele.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IQueryHandler<CalculateNtnbPriceQuery, NtnbPriceResult>>();
                services.AddScoped(_ => _handler);
            });
        });
    }

    [Fact(DisplayName = "Query calculateNtnbPrice retorna o preço calculado pelo input")]
    public async Task CalculateNtnbPrice_WithValidInput_ReturnsPrice()
    {
        // Arrange
        _handler.HandleAsync(Arg.Any<CalculateNtnbPriceQuery>(), Arg.Any<CancellationToken>())
            .Returns(new NtnbPriceResult(new DateOnly(2024, 5, 23), 4321.123456m, 2700, 3850.654321m));

        var client = _factory.CreateClient();
        var request = new
        {
            query = "query { calculateNtnbPrice(input: { vnaBase: 4300, yield: 0.07, inflation: 0.0046, " +
                    "tradeDate: \"2024-05-21\", dueDate: \"2035-05-15\" }) " +
                    "{ settlementDate businessDaysToMaturity unitPrice } }"
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var price = document.RootElement.GetProperty("data").GetProperty("calculateNtnbPrice");
        Assert.Equal("2024-05-23", price.GetProperty("settlementDate").GetString());
        Assert.Equal(2700, price.GetProperty("businessDaysToMaturity").GetInt32());
    }

    [Fact(DisplayName = "Query calculateNtnbPrice retorna erro de domínio para VNA inválido")]
    public async Task CalculateNtnbPrice_WithInvalidVna_ReturnsDomainErrorCode()
    {
        // Arrange — vnaBase 0 faz o mapper lançar InvalidNominalValueException antes do handler
        var client = _factory.CreateClient();
        var request = new
        {
            query = "query { calculateNtnbPrice(input: { vnaBase: 0, yield: 0.07, inflation: 0.0046, " +
                    "tradeDate: \"2024-05-21\", dueDate: \"2035-05-15\" }) { unitPrice } }"
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = document.RootElement.GetProperty("errors")[0];
        var code = error.GetProperty("extensions").GetProperty("code").GetString();
        Assert.Equal("analytics.nominalvalue.invalid", code);
    }
}
