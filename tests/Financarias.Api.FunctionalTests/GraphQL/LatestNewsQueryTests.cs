using System.Net.Http.Json;
using System.Text.Json;
using Financarias.Application.News;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class LatestNewsQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly INewsFeedGateway _gateway = Substitute.For<INewsFeedGateway>();
    private readonly WebApplicationFactory<Program> _factory;

    public LatestNewsQueryTests(WebApplicationFactory<Program> factory)
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
                services.RemoveAll<INewsFeedGateway>();
                services.AddScoped(_ => _gateway);
            });
        });
    }

    private static NewsArticle Article(string title, string? section, params string[] tags) =>
        new(title, $"https://x/{title}", "resumo", "<p>resumo</p>", section, tags,
            new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero));

    [Fact(DisplayName = "Query latestNews sem filtro retorna as notícias mapeadas")]
    public async Task LatestNews_WithoutFilter_ReturnsMappedArticles()
    {
        // Arrange
        _gateway.GetLatestAsync(Arg.Any<CancellationToken>())
            .Returns(new List<NewsArticle> { Article("Petróleo", "Mercados", "Opep+", "Petróleo") });

        var client = _factory.CreateClient();
        var request = new { query = "{ latestNews { title section tags summary summaryHtml } }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var article = document.RootElement.GetProperty("data").GetProperty("latestNews")[0];
        Assert.Equal("Petróleo", article.GetProperty("title").GetString());
        Assert.Equal("Mercados", article.GetProperty("section").GetString());
        Assert.Equal("Opep+", article.GetProperty("tags")[0].GetString());
    }

    [Fact(DisplayName = "Query latestNews filtra pela seção informada")]
    public async Task LatestNews_WithSection_FiltersBySection()
    {
        // Arrange
        _gateway.GetLatestAsync(Arg.Any<CancellationToken>())
            .Returns(new List<NewsArticle>
            {
                Article("de mercado", "Mercados"),
                Article("de esporte", "Esportes")
            });

        var client = _factory.CreateClient();
        var request = new { query = "{ latestNews(sections: [MERCADOS]) { title section } }" };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);

        // Assert
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var news = document.RootElement.GetProperty("data").GetProperty("latestNews");
        Assert.Equal(1, news.GetArrayLength());
        Assert.Equal("de mercado", news[0].GetProperty("title").GetString());
    }
}
