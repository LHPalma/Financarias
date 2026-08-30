namespace Financarias.Domain.MarketData.Fuel;

public static class EthanolGasolineParity
{
    private const decimal AdvantageousThreshold = 0.7m;

    public static decimal Ratio(decimal ethanolPrice, decimal gasolinePrice)
        => ethanolPrice / gasolinePrice;

    public static bool IsEthanolAdvantageous(decimal ratio)
        => ratio < AdvantageousThreshold;
}
