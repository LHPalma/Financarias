namespace Financarias.Integrations.Anbima.Holidays;

public sealed class AnbimaOptions
{
    public const string SectionName = "Integrations:Anbima";

    public string HolidaysFilePath { get; init; } = string.Empty;
}