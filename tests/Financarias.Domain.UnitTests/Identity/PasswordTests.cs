using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Identity;

namespace Financarias.Domain.UnitTests.Identity;

public class PasswordTests
{
    [Fact(DisplayName = "O conjunto de especiais é exatamente o ASCII imprimível não alfanumérico")]
    public void SpecialCharacters_MatchesPrintableAsciiSymbols()
    {
        // Arrange: o literal codifica uma regra, então é a regra que o teste afirma —
        // um símbolo esquecido só apareceria quando alguém escolhesse justo ele
        var expected = Enumerable.Range('!', '~' - '!' + 1)
            .Select(code => (char)code)
            .Where(character => !char.IsLetterOrDigit(character));

        // Act & Assert
        Assert.Equal(expected, Password.SpecialCharacters.OrderBy(character => character));
        Assert.Equal(Password.SpecialCharacters.Length, Password.SpecialCharacters.Distinct().Count());
    }

    [Fact(DisplayName = "O espaço não conta como especial, mas é aceito na senha")]
    public void Space_IsAcceptedButDoesNotSatisfyTheSpecialRule()
    {
        // Act & Assert: frase de senha precisa poder existir; espaço no fim não pode
        // transformar uma senha fraca em "forte"
        Assert.DoesNotContain(' ', Password.SpecialCharacters);

        var comEspaco = Password.Create("Senha Boa 1!");

        Assert.Equal("Senha Boa 1!", comEspaco.Value);
    }

    [Theory(DisplayName = "Create aceita senha que cumpre as quatro classes")]
    [InlineData("Abcdef1!")]
    [InlineData("S3nh@Forte")]
    [InlineData("  A1b!cdef  ")]
    public void Create_AcceptsCompliantPassword(string input)
    {
        // Act
        var password = Password.Create(input);

        // Assert: nada de Trim — cortar espaço da ponta mudaria a credencial
        Assert.Equal(input, password.Value);
    }

    [Theory(DisplayName = "Create lança o código da regra que falhou")]
    [InlineData(null, "identity.password.tooshort")]
    [InlineData("", "identity.password.tooshort")]
    [InlineData("Ab1!", "identity.password.tooshort")]
    [InlineData("abcdef1!", "identity.password.missinguppercase")]
    [InlineData("ABCDEF1!", "identity.password.missinglowercase")]
    [InlineData("Abcdefg!", "identity.password.missingdigit")]
    [InlineData("Abcdefg1", "identity.password.missingspecial")]
    public void Create_Throws_WithTheCodeOfTheBrokenRule(string? input, string expectedCode)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => Password.Create(input));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact(DisplayName = "Create lança quando a senha passa de 128 caracteres")]
    public void Create_Throws_WhenLongerThanMaximum()
    {
        // Arrange: 129 caracteres cumprindo todas as classes
        var input = $"Ab1!{new string('x', 125)}";

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => Password.Create(input));

        Assert.Equal("identity.password.toolong", exception.Code);
    }

    [Fact(DisplayName = "ToString não vaza a senha — aqui é texto puro, não hash")]
    public void ToString_DoesNotLeakThePassword()
    {
        // Act
        var rendered = Password.Create("Abcdef1!").ToString();

        // Assert
        Assert.DoesNotContain("Abcdef1!", rendered);
    }
}
