using Financarias.Integrations.MarketData.Google.Trends.Clients;
using Financarias.Integrations.MarketData.Google.Trends.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.MarketData.Google.Trends.Providers;

public class GoogleTrendsSearchInterestProviderTests
{
    private const string Explore =
        ")]}',\n{\"widgets\":[{\"request\":{\"time\":\"2023-01-01 2023-01-03\",\"resolution\":\"DAY\",\"locale\":\"pt-BR\"},\"id\":\"TIMESERIES\",\"token\":\"tok-1\",\"type\":\"fe_line_chart\"},{\"id\":\"GEO_MAP\",\"token\":\"tok-2\",\"request\":{}}]}";

    private const string Timeline =
        ")]}',\n{\"default\":{\"timelineData\":[" +
        "{\"time\":\"1672531200\",\"formattedTime\":\"1 de jan. de 2023\",\"value\":[54,18],\"hasData\":[true,true]}," +
        "{\"time\":\"1672617600\",\"formattedTime\":\"2 de jan. de 2023\",\"value\":[93,40],\"hasData\":[true,true]}," +
        "{\"time\":\"1672704000\",\"formattedTime\":\"3 de jan. de 2023\",\"value\":[100,42],\"hasData\":[true,true],\"isPartial\":true}" +
        "],\"averages\":[82,33]}}";

    private readonly IGoogleTrendsClient _client = Substitute.For<IGoogleTrendsClient>();
    private readonly GoogleTrendsSearchInterestProvider _provider;

    public GoogleTrendsSearchInterestProviderTests()
    {
        _provider = new GoogleTrendsSearchInterestProvider(_client, NullLogger<GoogleTrendsSearchInterestProvider>.Instance);
        _client.OpenSessionAsync(default!, default).ReturnsForAnyArgs(string.Empty);
        _client.ExploreAsync(default!, default, default!, default).ReturnsForAnyArgs(Explore);
        _client.GetTimelineAsync(default!, default, default!, default!, default).ReturnsForAnyArgs(Timeline);
    }

    [Fact(DisplayName = "GetAsync devolve um ponto por termo e dia, na ordem dos termos pedidos, e pula o ponto parcial")]
    public async Task Get_ReturnsOnePointPerTermAndDay_SkippingThePartialPoint()
    {
        // Act
        var items = await _provider.GetAsync(["americanas", "magalu"], "BR", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 3));

        // Assert
        Assert.Equal(4, items.Count);
        Assert.Equal(("americanas", new DateOnly(2023, 1, 1), 54), (items[0].Term, items[0].Day, items[0].Value));
        Assert.Equal(("magalu", new DateOnly(2023, 1, 1), 18), (items[1].Term, items[1].Day, items[1].Value));
        Assert.Equal(("americanas", new DateOnly(2023, 1, 2), 93), (items[2].Term, items[2].Day, items[2].Value));
        Assert.Equal(40, items[3].Value);
    }

    [Fact(DisplayName = "GetAsync abre a sessão antes de consultar e repassa o pedido e o token do widget da série")]
    public async Task Get_OpensTheSessionFirst_AndForwardsTheTimeseriesWidget()
    {
        // Act
        await _provider.GetAsync(["americanas"], "BR", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 3));

        // Assert
        Received.InOrder(() =>
        {
            _client.OpenSessionAsync("BR", Arg.Any<CancellationToken>());
            _client.ExploreAsync("pt-BR", 180, Arg.Is<string>(r => r.Contains("\"keyword\":\"americanas\"") && r.Contains("2023-01-01 2023-01-03")), Arg.Any<CancellationToken>());
            _client.GetTimelineAsync("pt-BR", 180, Arg.Is<string>(r => r.Contains("\"resolution\":\"DAY\"")), "tok-1", Arg.Any<CancellationToken>());
        });
    }

    [Fact(DisplayName = "GetAsync falha com mensagem clara quando a resposta não tem o widget da série")]
    public async Task Get_WithoutTimeseriesWidget_Throws()
    {
        // Arrange
        _client.ExploreAsync(default!, default, default!, default).ReturnsForAnyArgs(")]}',\n{\"widgets\":[{\"id\":\"GEO_MAP\",\"token\":\"x\",\"request\":{}}]}");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _provider.GetAsync(["americanas"], "BR", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 3)));

        Assert.Contains("time series widget", exception.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "GetAsync falha quando o Google responde com algo que não é JSON, como uma página de erro")]
    public async Task Get_WithNonJsonAnswer_Throws()
    {
        // Arrange
        _client.ExploreAsync(default!, default, default!, default).ReturnsForAnyArgs("<html>Error 429</html>");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _provider.GetAsync(["americanas"], "BR", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 3)));
    }

    [Fact(DisplayName = "GetAsync devolve só os termos que a resposta traz quando vêm menos valores que termos")]
    public async Task Get_WithFewerValuesThanTerms_ReadsWhatArrived()
    {
        // Act
        var items = await _provider.GetAsync(["a", "b", "c"], "BR", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 3));

        // Assert
        Assert.Equal(4, items.Count);
        Assert.DoesNotContain(items, item => item.Term == "c");
    }
}
