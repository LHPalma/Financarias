using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Holidays.Exceptions;

public static class HolidayErrors
{
    public const string NameRequired = "holiday.name.required";

    public static DomainValidationException Name() =>
        new(NameRequired, "Holiday name is required.");
}
