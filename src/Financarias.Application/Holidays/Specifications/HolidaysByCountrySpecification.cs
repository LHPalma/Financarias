using Ardalis.Specification;
using Financarias.Domain.Holidays.Models;

namespace Financarias.Application.Holidays.Specifications;

public sealed class HolidaysByCountrySpecification : Specification<Holiday>
{
    public HolidaysByCountrySpecification(CountryCode countryCode)
    {
        Query.Where(holiday => holiday.CountryCode == countryCode);
    }
}
