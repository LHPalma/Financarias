using Financarias.Domain.Common;
using Financarias.Domain.Holidays.Exceptions;

namespace Financarias.Domain.Holidays;

public sealed class Holiday : BaseEntity<long>, IAggregateRoot
{
    private Holiday()
    {
    }

    private Holiday(DateOnly date, string name, CountryCode countryCode)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidHolidayNameException();
        }

        Date = date;
        Name = name;
        CountryCode = countryCode;
    }

    public DateOnly Date { get; private set; }

    public string Name { get; private set; } = null!;

    public CountryCode CountryCode { get; private set; }

    public static Holiday Create(DateOnly date, string name, CountryCode countryCode) =>
        new(date, name, countryCode);
}
