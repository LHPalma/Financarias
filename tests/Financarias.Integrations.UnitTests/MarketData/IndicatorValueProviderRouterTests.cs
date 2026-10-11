using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData;
using Financarias.Integrations.MarketData.Bcb.Sgs.Clients;
using Financarias.Integrations.MarketData.Bcb.Sgs.Providers;
using Financarias.Integrations.MarketData.Cboe.Clients;
using Financarias.Integrations.MarketData.Cboe.Providers;
using Financarias.Integrations.MarketData.Eia.Clients;
using Financarias.Integrations.MarketData.Eia.Providers;
using Financarias.Integrations.MarketData.Fed.Clients;
using Financarias.Integrations.MarketData.Fed.Providers;
using Financarias.Integrations.MarketData.WorldBank.Clients;
using Financarias.Integrations.MarketData.WorldBank.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.MarketData;

public class IndicatorValueProviderRouterTests
{
    private readonly IBcbSgsClient _bcb = Substitute.For<IBcbSgsClient>();
    private readonly IEiaClient _eia = Substitute.For<IEiaClient>();
    private readonly ICboeClient _cboe = Substitute.For<ICboeClient>();
    private readonly IFedDataDownloadClient _fed = Substitute.For<IFedDataDownloadClient>();
    private readonly IWorldBankPageClient _worldBankPage = Substitute.For<IWorldBankPageClient>();
    private readonly IWorldBankDocsClient _worldBankDocs = Substitute.For<IWorldBankDocsClient>();
    private readonly IndicatorValueProviderRouter _router;

    public IndicatorValueProviderRouterTests()
    {
        _bcb.GetSeriesAsync(default, default!, default!, default).ReturnsForAnyArgs([]);
        _eia.GetSpotPricesFileAsync(default!, default).ReturnsForAnyArgs(_ => Task.FromResult<Stream>(new MemoryStream([0])));
        _cboe.GetVixHistoryAsync(default).ReturnsForAnyArgs("DATE,OPEN,HIGH,LOW,CLOSE\n");
        _fed.GetSeriesAsync(default!, default!, default!, default).ReturnsForAnyArgs("\"Time Period\",\"X\"\n");
        _worldBankPage.GetCommodityMarketsPageAsync(default).ReturnsForAnyArgs("<html></html>");

        _router = new IndicatorValueProviderRouter(
            new BcbIndicatorValueProvider(_bcb, TimeProvider.System, NullLogger<BcbIndicatorValueProvider>.Instance),
            new EiaIndicatorValueProvider(_eia, NullLogger<EiaIndicatorValueProvider>.Instance),
            new CboeIndicatorValueProvider(_cboe, NullLogger<CboeIndicatorValueProvider>.Instance),
            new FedDollarIndexValueProvider(_fed, NullLogger<FedDollarIndexValueProvider>.Instance),
            new WorldBankIndicatorValueProvider(_worldBankPage, _worldBankDocs, NullLogger<WorldBankIndicatorValueProvider>.Instance));
    }

    [Theory(DisplayName = "Indicadores do Banco Central vão para o SGS")]
    [InlineData(EconomicIndicator.Dollar)]
    [InlineData(EconomicIndicator.SelicTarget)]
    [InlineData(EconomicIndicator.Cdi)]
    [InlineData(EconomicIndicator.Ipca)]
    public async Task GetYear_RoutesCentralBankIndicatorsToSgs(EconomicIndicator indicator)
    {
        // Act
        await _router.GetYearAsync(indicator, 2026);

        // Assert
        await _bcb.ReceivedWithAnyArgs(1).GetSeriesAsync(default, default!, default!, default);
    }

    [Theory(DisplayName = "Brent e WTI vão para a EIA")]
    [InlineData(EconomicIndicator.BrentOil, "RBRTEd.xls")]
    [InlineData(EconomicIndicator.WtiOil, "RWTCd.xls")]
    public async Task GetYear_RoutesOilToEia(EconomicIndicator indicator, string file)
    {
        // Act: o conteúdo de teste não é uma planilha, então a leitura falha depois de a EIA ter sido chamada
        await Assert.ThrowsAnyAsync<Exception>(() => _router.GetYearAsync(indicator, 2026));

        // Assert
        await _eia.Received(1).GetSpotPricesFileAsync(file, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "O VIX vai para a Cboe")]
    public async Task GetYear_RoutesVixToCboe()
    {
        // Act
        await _router.GetYearAsync(EconomicIndicator.Vix, 2026);

        // Assert
        await _cboe.ReceivedWithAnyArgs(1).GetVixHistoryAsync(default);
    }

    [Fact(DisplayName = "O índice do dólar vai para o Federal Reserve")]
    public async Task GetYear_RoutesDollarIndexToTheFed()
    {
        // Act
        await _router.GetYearAsync(EconomicIndicator.DollarIndex, 2026);

        // Assert
        await _fed.ReceivedWithAnyArgs(1).GetSeriesAsync(default!, default!, default!, default);
    }

    [Fact(DisplayName = "O minério de ferro vai para o Banco Mundial")]
    public async Task GetYear_RoutesIronOreToTheWorldBank()
    {
        // Act
        await _router.GetYearAsync(EconomicIndicator.IronOre, 2026);

        // Assert
        await _worldBankPage.ReceivedWithAnyArgs(1).GetCommodityMarketsPageAsync(default);
    }

    [Fact(DisplayName = "Todo valor do enum tem uma rota: nenhum indicador fica sem provedor")]
    public async Task GetYear_EveryIndicatorHasAProvider()
    {
        foreach (var indicator in Enum.GetValues<EconomicIndicator>())
        {
            // Act
            var exception = await Record.ExceptionAsync(() => _router.GetYearAsync(indicator, 2026));

            // Assert: pode falhar ao ler o arquivo de teste, mas nunca por falta de rota ou de provedor errado
            Assert.IsNotType<ArgumentOutOfRangeException>(exception);
        }
    }
}
