using System.Security.Cryptography;
using Financarias.Api.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Financarias.Api.FunctionalTests.Security;

public class JwtOptionsTests
{
    private static readonly string StrongKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    [Fact(DisplayName = "Configuração válida passa na validação de boot")]
    public void StartupValidation_Passes_ForValidConfiguration()
    {
        // Arrange
        var validator = StartupValidatorFor(ValidSettings());

        // Act
        var exception = Record.Exception(validator.Validate);

        // Assert
        Assert.Null(exception);
    }

    [Theory(DisplayName = "Issuer ou audience ausente derruba o boot dizendo qual")]
    [InlineData("Jwt:Issuer", "Jwt:Issuer não configurado")]
    [InlineData("Jwt:Audience", "Jwt:Audience não configurado")]
    public void StartupValidation_Fails_WhenIssuerOrAudienceIsMissing(string key, string expectedMessage)
    {
        // Arrange
        var settings = ValidSettings();
        settings.Remove(key);

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(StartupValidatorFor(settings).Validate);

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Theory(DisplayName = "Chave de assinatura fraca derruba o boot")]
    [InlineData("")]
    [InlineData("nao-e-base64!")]
    [InlineData("abcdefghijklmnopqrstuvwxyzABCDEF")]
    public void StartupValidation_Fails_ForWeakSigningKey(string signingKey)
    {
        // Arrange: o terceiro caso tem 32 caracteres base64 válidos, mas decodifica para só 24 bytes —
        // contar caracteres aceitaria uma chave abaixo dos 256 bits que o HS256 exige
        var settings = ValidSettings();
        settings["Jwt:SigningKey"] = signingKey;

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(StartupValidatorFor(settings).Validate);

        Assert.Contains("Jwt:SigningKey precisa ser base64 de ao menos 32 bytes", exception.Message);
    }

    [Theory(DisplayName = "Validade zero ou negativa derruba o boot")]
    [InlineData("00:00:00")]
    [InlineData("-00:05:00")]
    public void StartupValidation_Fails_ForNonPositiveLifetime(string lifetime)
    {
        // Arrange
        var settings = ValidSettings();
        settings["Jwt:AccessTokenLifetime"] = lifetime;

        // Act & Assert
        var exception = Assert.Throws<OptionsValidationException>(StartupValidatorFor(settings).Validate);

        Assert.Contains("Jwt:AccessTokenLifetime precisa ser positivo", exception.Message);
    }

    [Fact(DisplayName = "Sem validade configurada, o padrão é uma hora")]
    public void Options_DefaultsToOneHourLifetime()
    {
        // Arrange
        var settings = ValidSettings();
        settings.Remove("Jwt:AccessTokenLifetime");

        // Act
        var options = ServicesFor(settings).GetRequiredService<IOptions<JwtOptions>>().Value;

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), options.AccessTokenLifetime);
    }

    private static Dictionary<string, string?> ValidSettings() => new()
    {
        ["Jwt:Issuer"] = "financarias",
        ["Jwt:Audience"] = "financarias-api",
        ["Jwt:SigningKey"] = StrongKey,
        ["Jwt:AccessTokenLifetime"] = "01:00:00"
    };

    private static IStartupValidator StartupValidatorFor(Dictionary<string, string?> settings) =>
        ServicesFor(settings).GetRequiredService<IStartupValidator>();

    private static ServiceProvider ServicesFor(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddAccessTokens(configuration)
            .BuildServiceProvider();
    }
}
