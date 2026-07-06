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

public class CurrencyPricesQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IFxRateGateway _gateway = Substitute.For<IFxRateGateway>();
    private readonly WebApplicationFactory<Program> _factory;

    public CurrencyPricesQueryTests(WebApplicationFactory<Program> factory)
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

    [Fact(DisplayName = "Query currencyPrices retorna o preço em BRL das moedas pedidas")]
    public async Task CurrencyPrices_ReturnsPricesInBrl()
    {
        // Arrange: 1 BRL = 0,20 USD -> 1 USD = 5,00 BRL
        _gateway.GetLatestRatesAsync(Arg.Any<Currency>(), Arg.Any<CancellationToken>())
            .Returns(new FxRateSnapshot(
                Currency.Brl,
                new Dictionary<Currency, decimal> { [Currency.Brl] = 1m, [Currency.Usd] = 0.20m },
                new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero)));

        var client = _factory.CreateClient();
        var request = new { query = "{ currencyPrices(currencies: [USD]) { currency quote price } }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var price = document.RootElement.GetProperty("data").GetProperty("currencyPrices")[0];
        Assert.Equal("USD", price.GetProperty("currency").GetString());
        Assert.Equal("BRL", price.GetProperty("quote").GetString());
        Assert.Equal(5.00m, price.GetProperty("price").GetDecimal());
    }
}
