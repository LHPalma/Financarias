using System.Text;
using Financarias.Integrations.News.InfoMoney.Clients;
using Financarias.Integrations.News.InfoMoney.Providers;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.News.InfoMoney.Providers;

public class InfoMoneyNewsProviderTests
{
    private readonly IInfoMoneyClient _client = Substitute.For<IInfoMoneyClient>();

    private const string OneItemFeed =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <title>InfoMoney</title>
            <link>https://www.infomoney.com.br</link>
            <description>Feed</description>
            <item>
              <title>Petróleo abre em queda</title>
              <link>https://www.infomoney.com.br/mercados/petroleo-abre-em-queda/</link>
              <pubDate>Sun, 05 Jul 2026 23:36:55 +0000</pubDate>
              <category><![CDATA[Mercados]]></category>
              <category><![CDATA[Opep+]]></category>
              <category><![CDATA[Petróleo]]></category>
              <description><![CDATA[<p><img src="x.jpg" />O grupo de produtores concordou em aumentar as cotas.</p>
              <p>The post <a href="https://x">Petróleo abre em queda</a> appeared first on <a href="https://www.infomoney.com.br">InfoMoney</a>.</p>]]></description>
            </item>
          </channel>
        </rss>
        """;

    private void FeedReturns(string rss) =>
        _client.GetFeedAsync(Arg.Any<CancellationToken>())
            .Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(rss)));

    [Fact(DisplayName = "Mapeia título, link e data, e separa a seção das tags")]
    public async Task GetLatest_WithFeed_MapsFieldsAndSplitsSectionFromTags()
    {
        // Arrange
        FeedReturns(OneItemFeed);
        var provider = new InfoMoneyNewsProvider(_client);

        // Act
        var article = Assert.Single(await provider.GetLatestAsync());

        // Assert
        Assert.Equal("Petróleo abre em queda", article.Title);
        Assert.Equal("https://www.infomoney.com.br/mercados/petroleo-abre-em-queda/", article.Link);
        Assert.Equal("Mercados", article.Section);
        Assert.Equal(new[] { "Opep+", "Petróleo" }, article.Tags);
        Assert.Equal(new DateTimeOffset(2026, 7, 5, 23, 36, 55, TimeSpan.Zero), article.PublishedAt);
    }

    [Fact(DisplayName = "Limpa o HTML do resumo e remove o boilerplate, mantendo o HTML cru")]
    public async Task GetLatest_WithHtmlSummary_CleansSummaryAndKeepsRawHtml()
    {
        // Arrange
        FeedReturns(OneItemFeed);
        var provider = new InfoMoneyNewsProvider(_client);

        // Act
        var article = Assert.Single(await provider.GetLatestAsync());

        // Assert
        Assert.Equal("O grupo de produtores concordou em aumentar as cotas.", article.Summary);
        Assert.DoesNotContain("appeared first on", article.Summary);
        Assert.Contains("<img", article.SummaryHtml);
    }

    [Fact(DisplayName = "Item sem data de publicação retorna PublishedAt nulo")]
    public async Task GetLatest_WithoutPubDate_ReturnsNullPublishedAt()
    {
        // Arrange
        FeedReturns(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0">
              <channel>
                <title>InfoMoney</title>
                <link>https://www.infomoney.com.br</link>
                <description>Feed</description>
                <item>
                  <title>Sem data</title>
                  <link>https://www.infomoney.com.br/mercados/sem-data/</link>
                  <category><![CDATA[Mercados]]></category>
                </item>
              </channel>
            </rss>
            """);
        var provider = new InfoMoneyNewsProvider(_client);

        // Act
        var article = Assert.Single(await provider.GetLatestAsync());

        // Assert
        Assert.Null(article.PublishedAt);
    }

    [Fact(DisplayName = "Feed sem itens retorna lista vazia")]
    public async Task GetLatest_WithEmptyFeed_ReturnsEmptyList()
    {
        // Arrange
        FeedReturns(
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0">
              <channel>
                <title>InfoMoney</title>
                <link>https://www.infomoney.com.br</link>
                <description>Feed</description>
              </channel>
            </rss>
            """);
        var provider = new InfoMoneyNewsProvider(_client);

        // Act
        var result = await provider.GetLatestAsync();

        // Assert
        Assert.Empty(result);
    }
}
