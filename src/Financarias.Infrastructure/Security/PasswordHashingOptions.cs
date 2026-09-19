namespace Financarias.Infrastructure.Security;

public sealed class PasswordHashingOptions
{
    public const string SectionName = "PasswordHashing";

    public int CurrentPepperVersion { get; init; }

    public Dictionary<int, string> Peppers { get; init; } = [];
}