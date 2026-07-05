using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.MarketData.Stocks.DTOs.Results;
using Financarias.Application.MarketData.Stocks.Gateways;
using Financarias.Domain.MarketData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class StockQuoteQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly IStockQuoteGateway _gateway = Substitute.For<IStockQuoteGateway>();
    private readonly WebApplicationFactory<Program> _factory;

    public StockQuoteQueryTests(WebApplicationFactory<Program> factory)
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
                services.RemoveAll<IStockQuoteGateway>();
                services.AddScoped(_ => _gateway);
            });
        });
    }

    [Fact(DisplayName = "Query stockQuote retorna a cotação resolvida pelo gateway")]
    public async Task StockQuote_WithResolvedQuote_ReturnsMappedData()
    {
        // Arrange
        _gateway
            .FindQuoteAsync(Ticker.Create("PETR4"), Arg.Any<CancellationToken>())
            .Returns(new StockQuoteResult(
                "PETR4", "PETROBRAS PN", "BRL", 38.25m, 0.29m, 0.76m,
                38.07m, 38.25m, 37.86m, 38.24m, 10359300L,
                new DateTimeOffset(2026, 7, 3, 21, 31, 30, TimeSpan.Zero)));

        var client = _factory.CreateClient();
        var request = new { query = "{ stockQuote(ticker: \"PETR4\") { symbol price currency change asOf } }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var quote = document.RootElement.GetProperty("data").GetProperty("stockQuote");
        Assert.Equal("PETR4", quote.GetProperty("symbol").GetString());
        Assert.Equal("BRL", quote.GetProperty("currency").GetString());
    }

    [Fact(DisplayName = "Query stockQuote retorna null quando o gateway não encontra a cotação (miss)")]
    public async Task StockQuote_WhenGatewayReturnsNull_ReturnsNull()
    {
        // Arrange
        _gateway
            .FindQuoteAsync(Arg.Any<Ticker>(), Arg.Any<CancellationToken>())
            .Returns((StockQuoteResult?)null);

        var client = _factory.CreateClient();
        var request = new { query = "{ stockQuote(ticker: \"XXXX4\") { symbol } }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var stockQuote = document.RootElement.GetProperty("data").GetProperty("stockQuote");
        Assert.Equal(JsonValueKind.Null, stockQuote.ValueKind);
    }

    [Fact(DisplayName = "Query stockQuote retorna erro com code stock.ticker.invalid para ticker inválido")]
    public async Task StockQuote_WithInvalidTicker_ReturnsDomainErrorCode()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { query = "{ stockQuote(ticker: \"PETR\") { symbol } }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = document.RootElement.GetProperty("errors")[0];
        Assert.Equal("stock.ticker.invalid", error.GetProperty("extensions").GetProperty("code").GetString());
    }
}
