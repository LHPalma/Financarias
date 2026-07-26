using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Financarias.Api.FunctionalTests.GraphQL;

public class SimulateFinancingQueryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SimulateFinancingQueryTests(WebApplicationFactory<Program> factory)
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

    [Fact(DisplayName = "Query simulateFinancing devolve a parcela e a tabela de evolução")]
    public async Task SimulateFinancing_WithValidInput_ReturnsInstallmentAndSchedule()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            query = "query { simulateFinancing(input: { principal: 30000, monthlyRate: 0.015, installments: 48 }) " +
                    "{ installment totalPaid totalInterest schedule { period installment interest " +
                    "accumulatedInterest amortization outstandingBalance } } }"
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);
        var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var result = payload!.RootElement.GetProperty("data").GetProperty("simulateFinancing");
        var schedule = result.GetProperty("schedule");

        // Assert
        Assert.Equal(881.25m, result.GetProperty("installment").GetDecimal());
        Assert.Equal(42300.03m, result.GetProperty("totalPaid").GetDecimal());
        Assert.Equal(12300.03m, result.GetProperty("totalInterest").GetDecimal());
        Assert.Equal(48, schedule.GetArrayLength());
        Assert.Equal(450.00m, schedule[0].GetProperty("interest").GetDecimal());
        Assert.Equal(450.00m, schedule[0].GetProperty("accumulatedInterest").GetDecimal());
        Assert.Equal(881.28m, schedule[47].GetProperty("installment").GetDecimal());
        Assert.Equal(12300.03m, schedule[47].GetProperty("accumulatedInterest").GetDecimal());
        Assert.Equal(0m, schedule[47].GetProperty("outstandingBalance").GetDecimal());
    }

    [Fact(DisplayName = "Entrada inválida vira erro GraphQL com o código de domínio")]
    public async Task SimulateFinancing_WithNegativeRate_ReturnsDomainErrorCode()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            query = "query { simulateFinancing(input: { principal: 30000, monthlyRate: -0.01, installments: 48 }) " +
                    "{ installment } }"
        };

        // Act
        var response = await client.PostAsJsonAsync("/graphql", request);
        var payload = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var error = payload!.RootElement.GetProperty("errors")[0];

        // Assert
        Assert.Equal(
            "analytics.monthlyrate.invalid",
            error.GetProperty("extensions").GetProperty("code").GetString());
    }
}
