using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Identity;

namespace Financarias.Domain.UnitTests.Identity;

public class PasswordHashTests
{
    private const string Phc =
        "$argon2id$v=19$m=19456,t=2,p=1$HW3YsW3S8OjO0KmcoBup4Q$66wDuOrd5mxXx3qUkgheiEqwf1VRa3+OuK2OZD0Dm0I";

    [Fact(DisplayName = "Create preserva a string PHC intacta")]
    public void Create_KeepsThePhcStringUntouched()
    {
        // Act
        var hash = PasswordHash.Create(Phc);

        // Assert
        Assert.Equal(Phc, hash.Value);
    }

    [Theory(DisplayName = "Create lança com o código de hash inválido")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("senha-em-texto-puro")]
    [InlineData("$argon2id$poucos$cifroes")]
    [InlineData("$argon2id$v=19$m=19456,t=2,p=1$sal com espaco$hash")]
    public void Create_Throws_ForInputThatIsNotPhc(string? input)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => PasswordHash.Create(input));

        Assert.Equal("identity.passwordhash.invalid", exception.Code);
    }

    [Fact(DisplayName = "TryCreate devolve false e nulo para texto puro")]
    public void TryCreate_ReturnsFalse_ForPlainText()
    {
        // Act
        var created = PasswordHash.TryCreate("minha-senha", out var hash);

        // Assert
        Assert.False(created);
        Assert.Null(hash);
    }

    [Fact(DisplayName = "Dois hashes com o mesmo valor são iguais")]
    public void Equals_ReturnsTrue_ForSameValue()
    {
        // Act & Assert
        Assert.Equal(PasswordHash.Create(Phc), PasswordHash.Create(Phc));
    }

    [Fact(DisplayName = "ToString não vaza o hash — material de credencial não vai para log")]
    public void ToString_DoesNotLeakTheHash()
    {
        // Act
        var rendered = PasswordHash.Create(Phc).ToString();

        // Assert
        Assert.DoesNotContain("argon2", rendered);
        Assert.DoesNotContain(Phc, rendered);
    }
}
