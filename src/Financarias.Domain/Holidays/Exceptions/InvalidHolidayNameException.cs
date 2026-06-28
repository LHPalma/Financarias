using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.Holidays.Exceptions;

public sealed class InvalidHolidayNameException()
    : BaseDomainException("holiday.name.required", "Holiday name is required.");
