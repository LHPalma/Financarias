using Financarias.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Financarias.Infrastructure.IntegrationTests.Security;

public class PasswordHashingOptionsTests
{
    private const string ValidPepper = "pepper-valido-com-pelo-menos-32-caracteres";

    [Fact(DisplayName = "Configuração válida passa na validação de boot")]
    public void StartupValidation_Passes_ForValidConfiguration()
    {
        // Arrange
        var validator = StartupValidatorFor(new Dictionary<string, string?>
        {
            ["PasswordHashing:CurrentPepperVersion"] = "1",
            ["PasswordHashing:Peppers:1"] = ValidPepper
        });

        // Act
        var exception = Record.Exception(validator.Validate);

        // Assert
        Assert.Null(exception);
    }

    [Fact(DisplayName = "Sem nenhum pepper configurado, o boot falha dizendo isso")]
    public void StartupValidation_Fails_WhenNoPepperIsConfigured()
    {
        // Arrange
        var validator = StartupValidatorFor(new Dictionary<string, string?>());

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(validator.Validate);

        Assert.Contains("PasswordHashing:Peppers não configurado", exception.Message);
    }

    [Fact(DisplayName = "Versão corrente ausente do mapa derruba o boot")]
    public void StartupValidation_Fails_WhenCurrentVersionIsMissing()
    {
        // Arrange
        var validator = StartupValidatorFor(new Dictionary<string, string?>
        {
            ["PasswordHashing:CurrentPepperVersion"] = "2",
            ["PasswordHashing:Peppers:1"] = ValidPepper
        });

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(validator.Validate);

        Assert.Contains("CurrentPepperVersion não existe em Peppers", exception.Message);
    }

    [Theory(DisplayName = "Pepper vazio ou curto demais derruba o boot")]
    [InlineData("")]
    [InlineData("curto-demais")]
    public void StartupValidation_Fails_WhenAPepperIsTooShort(string pepper)
    {
        // Arrange
        var validator = StartupValidatorFor(new Dictionary<string, string?>
        {
            ["PasswordHashing:CurrentPepperVersion"] = "1",
            ["PasswordHashing:Peppers:1"] = ValidPepper,
            ["PasswordHashing:Peppers:2"] = pepper
        });

        // Act & Assert: o pepper ruim nem é o corrente — uma versão antiga fraca também é rejeitada
        var exception = Assert.Throws<OptionsValidationException>(validator.Validate);

        Assert.Contains("ao menos 32 caracteres", exception.Message);
    }

    private static IStartupValidator StartupValidatorFor(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider()
            .GetRequiredService<IStartupValidator>();
    }
}
