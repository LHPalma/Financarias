using System.Reflection;
using System.Text.RegularExpressions;
using Financarias.Domain.Common.Exceptions;

namespace Financarias.Domain.UnitTests.Common;

public class DomainErrorCatalogTests
{
    private static readonly IReadOnlyList<(string Catalog, string Field, string Code)> Catalog =
        typeof(BaseDomainException).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: true, IsSealed: true } && type.Name.EndsWith("Errors"))
            .SelectMany(type => type
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field is { IsLiteral: true } && field.FieldType == typeof(string))
                .Select(field => (type.Name, field.Name, (string)field.GetRawConstantValue()!)))
            .OrderBy(entry => entry.Item3, StringComparer.Ordinal)
            .ToList();

    [Fact(DisplayName = "Catálogo de códigos é a lista de chaves i18n — atualizar ao criar uma invariante")]
    public void Catalog_ListsEveryDomainErrorCode()
    {
        // Arrange — cada código vira uma chave de tradução; a lista é o contrato com o .resx
        string[] expected =
        [
            "address.cep.invalid",
            "analytics.businessdaycount.invalid",
            "analytics.financing.payoffperiod.invalid",
            "analytics.financing.unrepresentable",
            "analytics.installmentcount.invalid",
            "analytics.monthlyrate.invalid",
            "analytics.nominalvalue.invalid",
            "analytics.yield.invalid",
            "contacts.email.invalid",
            "fuel.price.invalid",
            "fuel.station.name.required",
            "holiday.name.required",
            "legalentity.cnpj.invalid",
            "stock.ticker.invalid",
            "identity.user.name.required",
            "identity.passwordhash.invalid",
            "identity.user.email.duplicate",
            "identity.user.notfound",
        ];

        // Act
        var codes = Catalog.Select(entry => entry.Code).ToList();

        var unregistered = codes.Except(expected).ToList();
        var stale = expected.Except(codes).ToList();

        // Assert
        Assert.True(
            unregistered.Count == 0 && stale.Count == 0,
            $"Códigos criados e não registrados: [{string.Join(", ", unregistered)}]. " +
            $"Códigos registrados que não existem mais: [{string.Join(", ", stale)}].");
    }

    [Fact(DisplayName = "Nenhum código é usado por duas invariantes diferentes")]
    public void Catalog_HasNoDuplicateCodes()
    {
        // Act
        var duplicates = Catalog
            .GroupBy(entry => entry.Code)
            .Where(group => group.Count() > 1)
            .Select(group =>
                $"{group.Key}: {string.Join(", ", group.Select(entry => $"{entry.Catalog}.{entry.Field}"))}")
            .ToList();

        // Assert
        Assert.Empty(duplicates);
    }

    [Theory(DisplayName = "Todo código segue a convenção <área>.<conceito>.<invariante>, minúsculo")]
    [MemberData(nameof(Codes))]
    public void Catalog_CodeFollowsNamingConvention(string code)
    {
        // Act & Assert
        Assert.Matches(new Regex(@"^[a-z]+(\.[a-z]+){2,}$"), code);
    }

    public static TheoryData<string> Codes
    {
        get
        {
            var data = new TheoryData<string>();

            foreach (var entry in Catalog)
            {
                data.Add(entry.Code);
            }

            return data;
        }
    }
}