using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.MarketData.ForeignExchange.DTOs.Results;
using Financarias.Application.MarketData.ForeignExchange.Gateways;
using Financarias.Domain.MarketData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class ConvertCurrencyQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IFxRateGateway _gateway = Substitute.For<IFxRateGateway>();
    private readonly WebApplicationFactory<Program> _factory;

    public ConvertCurrencyQueryTests(WebApplicationFactory<Program> factory)
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

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFxRateGateway>();
                services.AddScoped(_ => _gateway);
            });
        });
    }

    [Fact(DisplayName = "Query convertCurrency converte BRL->USD e devolve o valor arredondado")]
    public async Task ConvertCurrency_ConvertsAndRounds()
    {
        // Arrange
        _gateway.GetLatestRatesAsync(Arg.Any<Currency>(), Arg.Any<CancellationToken>())
            .Returns(new FxRateSnapshot(
                Currency.Brl,
                new Dictionary<Currency, decimal> { [Currency.Brl] = 1m, [Currency.Usd] = 0.20m },
                new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero)));

        var client = _factory.CreateClient();
        var request = new
        {
            query = "{ convertCurrency(amount: 1000, from: BRL, to: USD) { amount convertedAmount rate from to } }"
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var conversion = document.RootElement.GetProperty("data").GetProperty("convertCurrency");
        Assert.Equal("BRL", conversion.GetProperty("from").GetString());
        Assert.Equal("USD", conversion.GetProperty("to").GetString());
        Assert.Equal(200m, conversion.GetProperty("convertedAmount").GetDecimal());
    }

    [Fact(DisplayName = "Query convertCurrency retorna null quando a moeda não está na tabela")]
    public async Task ConvertCurrency_UnknownCurrency_ReturnsNull()
    {
        // Arrange
        _gateway.GetLatestRatesAsync(Arg.Any<Currency>(), Arg.Any<CancellationToken>())
            .Returns(new FxRateSnapshot(
                Currency.Brl,
                new Dictionary<Currency, decimal> { [Currency.Brl] = 1m },
                new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero)));

        var client = _factory.CreateClient();
        var request = new
        {
            query = "{ convertCurrency(amount: 10, from: BRL, to: JPY) { convertedAmount } }"
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var conversion = document.RootElement.GetProperty("data").GetProperty("convertCurrency");
        Assert.Equal(JsonValueKind.Null, conversion.ValueKind);
    }
}
