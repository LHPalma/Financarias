using Financarias.Application.Common.Messaging;
using Financarias.Domain.Holidays.Models;

namespace Financarias.Application.Holidays.Queries;

public record FindHolidaysInRangeQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    CountryCode CountryCode
) : IQuery<IReadOnlySet<DateOnly>>;