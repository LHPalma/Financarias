namespace Financarias.Domain.MarketData.ForeignExchange;

public static class CurrencyConverter
{
    public static decimal Convert(decimal amount, decimal rateFrom, decimal rateTo)
        => amount * CrossRate(rateFrom, rateTo);

    public static decimal CrossRate(decimal rateFrom, decimal rateTo)
        => rateTo / rateFrom;
}