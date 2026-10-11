using Financarias.Domain.MarketData.Indicators;
using Financarias.Integrations.MarketData;
using Financarias.Integrations.MarketData.Bcb.Sgs.Clients;
using Financarias.Integrations.MarketData.Bcb.Sgs.DTOs.Responses;
using Financarias.Integrations.MarketData.Bcb.Sgs.Providers;
using Financarias.Integrations.MarketData.Fred;
using Financarias.Integrations.MarketData.Fred.Clients;
using Financarias.Integrations.MarketData.Fred.DTOs.Responses;
using Financarias.Integrations.MarketData.Fred.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Financarias.Integrations.UnitTests.MarketData;

public class IndicatorValueProviderRouterTests
{
    private readonly IBcbSgsClient _bcbClient = Substitute.For<IBcbSgsClient>();
    private readonly IFredClient _fredClient = Substitute.For<IFredClient>();
    private readonly IndicatorValueProviderRouter _router;

    public IndicatorValueProviderRouterTests()
    {
        _bcbClient.GetSeriesAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([]);
        _fredClient.GetObservationsAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs(new FredObservationsResponse());

        _router = new IndicatorValueProviderRouter(
            new BcbIndicatorValueProvider(_bcbClient, TimeProvider.System, NullLogger<BcbIndicatorValueProvider>.Instance),
            new FredIndicatorValueProvider(_fredClient, Options.Create(new FredOptions { ApiKey = "test-key" }), NullLogger<FredIndicatorValueProvider>.Instance));
    }

    [Theory(DisplayName = "Indicadores do Banco Central vão para o SGS e não tocam o FRED")]
    [InlineData(EconomicIndicator.Dollar)]
    [InlineData(EconomicIndicator.SelicTarget)]
    [InlineData(EconomicIndicator.Cdi)]
    [InlineData(EconomicIndicator.Ipca)]
    public async Task GetYear_RoutesCentralBankIndicatorsToSgs(EconomicIndicator indicator)
    {
        // Act
        await _router.GetYearAsync(indicator, 2026);

        // Assert
        await _bcbClient.Received(1).GetSeriesAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _fredClient.DidNotReceiveWithAnyArgs().GetObservationsAsync(default!, default!, default!, default!, default);
    }

    [Theory(DisplayName = "Indicadores do mercado externo vão para o FRED e não tocam o SGS")]
    [InlineData(EconomicIndicator.BrentOil)]
    [InlineData(EconomicIndicator.WtiOil)]
    [InlineData(EconomicIndicator.IronOre)]
    [InlineData(EconomicIndicator.DollarIndex)]
    [InlineData(EconomicIndicator.Vix)]
    public async Task GetYear_RoutesExternalIndicatorsToFred(EconomicIndicator indicator)
    {
        // Act
        await _router.GetYearAsync(indicator, 2026);

        // Assert
        await _fredClient.Received(1).GetObservationsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _bcbClient.DidNotReceiveWithAnyArgs().GetSeriesAsync(default, default!, default!, default);
    }

    [Fact(DisplayName = "Todo valor do enum tem uma rota: nenhum indicador cai num provedor que o recusa")]
    public async Task GetYear_RoutesEveryIndicatorWithoutError()
    {
        foreach (var indicator in Enum.GetValues<EconomicIndicator>())
        {
            // Act & Assert
            await _router.GetYearAsync(indicator, 2026);
        }
    }
}
