using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class SimulateEarlyPayoffQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SimulateEarlyPayoffQueryTests(WebApplicationFactory<Program> factory)
    {
        // Sem stub: a simulação é cálculo puro, então o pipeline real roda ponta a ponta.
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Integrations:ViaCep:BaseUrl"] = "https://viacep.com.br/ws",
                    ["Integrations:Anbima:BaseUrl"] = "https://www.anbima.com.br"
                }));
        });
    }

    private async Task<JsonElement> PostAsync(string query)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/graphql", new { query });
        var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();

        return payload!.RootElement;
    }

    [Fact(DisplayName = "Query simulateEarlyPayoff devolve o saldo a quitar e a economia")]
    public async Task SimulateEarlyPayoff_WithValidInput_ReturnsPayoff()
    {
        // Act
        var root = await PostAsync(
            "query { simulateEarlyPayoff(input: { principal: 30000, monthlyRate: 0.015, " +
            "installments: 10, atInstallment: 5 }) " +
            "{ period outstandingBalance installmentsRemaining interestPaid interestSaved } }");

        var result = root.GetProperty("data").GetProperty("simulateEarlyPayoff");

        // Assert
        Assert.Equal(5, result.GetProperty("period").GetInt32());
        Assert.Equal(15558.04m, result.GetProperty("outstandingBalance").GetDecimal());
        Assert.Equal(5, result.GetProperty("installmentsRemaining").GetInt32());
        Assert.Equal(1823.19m, result.GetProperty("interestPaid").GetDecimal());
        Assert.Equal(707.06m, result.GetProperty("interestSaved").GetDecimal());
    }

    [Fact(DisplayName = "Parcela além do prazo vira erro GraphQL com o código de domínio")]
    public async Task SimulateEarlyPayoff_WithPeriodBeyondTerm_ReturnsDomainErrorCode()
    {
        // Act
        var root = await PostAsync(
            "query { simulateEarlyPayoff(input: { principal: 30000, monthlyRate: 0.015, " +
            "installments: 10, atInstallment: 11 }) { outstandingBalance } }");

        var error = root.GetProperty("errors")[0];

        // Assert
        Assert.Equal(
            "analytics.financing.payoffperiod.invalid",
            error.GetProperty("extensions").GetProperty("code").GetString());
    }
}
