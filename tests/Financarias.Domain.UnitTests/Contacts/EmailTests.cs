using Financarias.Domain.Common.Exceptions;
using Financarias.Domain.Contacts;

namespace Financarias.Domain.UnitTests.Contacts;

public class EmailTests
{
    [Theory(DisplayName = "Create normaliza a entrada para minúsculas e sem espaços nas pontas")]
    [InlineData("luiz@example.com", "luiz@example.com")]
    [InlineData("  luiz@example.com  ", "luiz@example.com")]
    [InlineData("Luiz@Example.COM", "luiz@example.com")]
    [InlineData("LUIZ.PALMA+tag@sub.example.com.br", "luiz.palma+tag@sub.example.com.br")]
    public void Create_NormalizesValue_ForValidInput(string input, string expected)
    {
        // Act
        var email = Email.Create(input);

        // Assert
        Assert.Equal(expected, email.Value);
    }

    [Theory(DisplayName = "Create lança com o código de e-mail inválido")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("luiz")]
    [InlineData("luiz@")]
    [InlineData("@example.com")]
    [InlineData("luiz@localhost")]
    [InlineData("Fulano <luiz@example.com>")]
    public void Create_Throws_ForInvalidInput(string? input)
    {
        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => Email.Create(input));

        Assert.Equal("contacts.email.invalid", exception.Code);
    }

    [Fact(DisplayName = "Create lança quando o endereço passa do limite de 254 caracteres")]
    public void Create_Throws_WhenLongerThanMaxLength()
    {
        // Arrange: 246 + "@example.com" (12) = 258 caracteres
        var input = $"{new string('a', 246)}@example.com";

        // Act & Assert
        var exception = Assert.Throws<DomainValidationException>(() => Email.Create(input));

        Assert.Equal("contacts.email.invalid", exception.Code);
    }

    [Fact(DisplayName = "TryCreate devolve true e o e-mail normalizado para entrada válida")]
    public void TryCreate_ReturnsTrue_ForValidInput()
    {
        // Act
        var created = Email.TryCreate("  Luiz@Example.com ", out var email);

        // Assert
        Assert.True(created);
        Assert.NotNull(email);
        Assert.Equal("luiz@example.com", email.Value);
    }

    [Fact(DisplayName = "TryCreate devolve false e nulo para entrada inválida")]
    public void TryCreate_ReturnsFalse_ForInvalidInput()
    {
        // Act
        var created = Email.TryCreate("nao-e-email", out var email);

        // Assert
        Assert.False(created);
        Assert.Null(email);
    }

    [Fact(DisplayName = "Dois e-mails que normalizam para o mesmo valor são iguais")]
    public void Equals_ReturnsTrue_ForSameNormalizedValue()
    {
        // Arrange
        var first = Email.Create("A@X.com");
        var second = Email.Create("  a@x.com  ");

        // Act & Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact(DisplayName = "ToString devolve o valor canônico")]
    public void ToString_ReturnsCanonicalValue()
    {
        // Arrange
        var email = Email.Create("Luiz@Example.com");

        // Act & Assert
        Assert.Equal("luiz@example.com", email.ToString());
    }
}
