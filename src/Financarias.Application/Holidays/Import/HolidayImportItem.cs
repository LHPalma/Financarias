using Financarias.Domain.Holidays.Models;

namespace Financarias.Application.Holidays.Import;

public sealed record HolidayImportItem(DateOnly Date, string Name, CountryCode CountryCode);