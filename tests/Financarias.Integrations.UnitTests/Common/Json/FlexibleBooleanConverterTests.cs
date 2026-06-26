using System.Text.Json;
using Financarias.Integrations.Common.Json;

namespace Financarias.Integrations.UnitTests.Common.Json;

public class FlexibleBooleanConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new FlexibleBooleanConverter() }
    };

    private static bool? Read(string json) => JsonSerializer.Deserialize<bool?>(json, Options);

    [Theory(DisplayName = "Lê boolean nativo e string booleana como bool?")]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("\"true\"", true)]
    [InlineData("\"false\"", false)]
    [InlineData("\"TRUE\"", true)]
    public void Read_WithBooleanOrBooleanString_ReturnsExpected(string json, bool expected)
    {
        // Act
        var result = Read(json);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory(DisplayName = "Retorna null para null e strings não-booleanas")]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"abc\"")]
    public void Read_WithNullOrInvalidString_ReturnsNull(string json)
    {
        // Act
        var result = Read(json);

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "Lança JsonException para token inesperado (número)")]
    public void Read_WithUnexpectedToken_ThrowsJsonException()
    {
        // Act + Assert
        Assert.Throws<JsonException>(() => Read("123"));
    }

    [Theory(DisplayName = "Escreve bool? como boolean JSON")]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void Write_WithValue_WritesJsonBoolean(bool value, string expected)
    {
        // Act
        var json = JsonSerializer.Serialize<bool?>(value, Options);

        // Assert
        Assert.Equal(expected, json);
    }
}
