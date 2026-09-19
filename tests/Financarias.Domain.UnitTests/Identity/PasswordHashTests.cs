using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Identity;

namespace Financarias.Domain.UnitTests.Identity;

public class PasswordHashTests
{
    private const string Phc =
        "$argon2id$v=19$m=19456,t=2,p=1$HW3YsW3S8OjO0KmcoBup4Q$66wDuOrd5mxXx3qUkgheiEqwf1VRa3+OuK2OZD0Dm0I";

    [Fact(DisplayName = "Create preserva a string PHC e a versão do pepper")]
    public void Create_KeepsValueAndPepperVersion()
    {
        // Act
        var hash = PasswordHash.Create(Phc, 3);

        // Assert
        Assert.Equal(Phc, hash.Value);
        Assert.Equal(3, hash.PepperVersion);
    }

    [Theory(DisplayName = "Create lança com o código de hash inválido quando a entrada não é PHC")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("senha-em-texto-puro")]
    [InlineData("$argon2id$poucos$cifroes")]
    [InlineData("$argon2id$v=19$m=19456,t=2,p=1$sal com espaco$hash")]
    public void Create_Throws_ForInputThatIsNotPhc(string? input)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => PasswordHash.Create(input, 1));

        Assert.Equal("identity.passwordhash.invalid", exception.Code);
    }

    [Theory(DisplayName = "Create lança quando a versão do pepper não é positiva")]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Throws_ForNonPositivePepperVersion(int pepperVersion)
    {
        // Act & Assert: versão zero é o valor que uma propriedade esquecida sem atribuição teria
        var exception = Assert.Throws<DomainValidationException>(() => PasswordHash.Create(Phc, pepperVersion));

        Assert.Equal("identity.passwordhash.invalid", exception.Code);
    }

    [Fact(DisplayName = "TryCreate devolve false e nulo para texto puro")]
    public void TryCreate_ReturnsFalse_ForPlainText()
    {
        // Act
        var created = PasswordHash.TryCreate("minha-senha", 1, out var hash);

        // Assert
        Assert.False(created);
        Assert.Null(hash);
    }

    [Fact(DisplayName = "Mesma string com a mesma versão é igual; com versão diferente, não")]
    public void Equals_ConsidersThePepperVersion()
    {
        // Act & Assert: sem o pepper da versão certa a string não verifica, então não é o mesmo valor
        Assert.Equal(PasswordHash.Create(Phc, 1), PasswordHash.Create(Phc, 1));
        Assert.NotEqual(PasswordHash.Create(Phc, 1), PasswordHash.Create(Phc, 2));
    }

    [Fact(DisplayName = "ToString não vaza o hash — material de credencial não vai para log")]
    public void ToString_DoesNotLeakTheHash()
    {
        // Act
        var rendered = PasswordHash.Create(Phc, 1).ToString();

        // Assert
        Assert.DoesNotContain("argon2", rendered);
        Assert.DoesNotContain(Phc, rendered);
    }
}
