using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.MarketData.Cryptos.DTOs.Results;
using Financarias.Application.MarketData.Cryptos.Gateways;
using Financarias.Domain.MarketData.Cryptos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class CryptoQuotesQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly ICryptoQuoteGateway _gateway = Substitute.For<ICryptoQuoteGateway>();
    private readonly WebApplicationFactory<Program> _factory;

    public CryptoQuotesQueryTests(WebApplicationFactory<Program> factory)
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
                services.RemoveAll<ICryptoQuoteGateway>();
                services.AddScoped(_ => _gateway);
            });
        });
    }

    [Fact(DisplayName = "Query cryptoQuotes retorna as cotações resolvidas pelo gateway")]
    public async Task CryptoQuotes_WithResolvedQuotes_ReturnsMappedData()
    {
        // Arrange
        _gateway
            .FindQuotesAsync(Arg.Any<IReadOnlyList<CryptoAsset>>(), QuoteCurrency.Brl, Arg.Any<CancellationToken>())
            .Returns(new List<CryptoQuoteResult>
            {
                new(CryptoAsset.Bitcoin, "btc", "Bitcoin", QuoteCurrency.Brl, 325234m,
                    null, null, null, null, null, null, new DateTimeOffset(2026, 7, 5, 4, 0, 0, TimeSpan.Zero)),
            });

        var client = _factory.CreateClient();
        var request = new { query = "{ cryptoQuotes(assets: [BITCOIN], currency: BRL) { asset symbol price currency } }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var quote = document.RootElement.GetProperty("data").GetProperty("cryptoQuotes")[0];
        Assert.Equal("BITCOIN", quote.GetProperty("asset").GetString());
        Assert.Equal("btc", quote.GetProperty("symbol").GetString());
        Assert.Equal("BRL", quote.GetProperty("currency").GetString());
    }

    [Fact(DisplayName = "Query availableCryptoAssets lista todos os ativos suportados")]
    public async Task AvailableCryptoAssets_ListsAllAssets()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new { query = "{ availableCryptoAssets }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var assets = document.RootElement.GetProperty("data").GetProperty("availableCryptoAssets");
        Assert.Equal(Enum.GetValues<CryptoAsset>().Length, assets.GetArrayLength());
    }
}
