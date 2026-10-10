namespace Financarias.Domain.MarketData;

public abstract record QuotePeriod
{
    public const int FirstYear = 1986;

    private QuotePeriod()
    {
    }

    public static QuotePeriod ForDay(DateOnly date) =>
        date.Year < FirstYear
            ? throw MarketDataErrors.Period(date.ToString("yyyy-MM-dd"))
            : new Daily(date);

    public static QuotePeriod ForMonth(int year, int month) =>
        year < FirstYear || month is < 1 or > 12
            ? throw MarketDataErrors.Period($"{year}-{month}")
            : new Monthly(year, month);

    public static QuotePeriod ForYear(int year) =>
        year < FirstYear
            ? throw MarketDataErrors.Period(year.ToString())
            : new Yearly(year);

    public sealed record Daily : QuotePeriod
    {
        internal Daily(DateOnly date) => Date = date;

        public DateOnly Date { get; }
    }

    public sealed record Monthly : QuotePeriod
    {
        internal Monthly(int year, int month)
        {
            Year = year;
            Month = month;
        }

        public int Year { get; }

        public int Month { get; }
    }

    public sealed record Yearly : QuotePeriod
    {
        internal Yearly(int year) => Year = year;

        public int Year { get; }
    }
}
